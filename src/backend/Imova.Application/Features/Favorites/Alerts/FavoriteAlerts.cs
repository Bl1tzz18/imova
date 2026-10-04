using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Emails;
using Imova.Application.Features.Listings;
using Imova.Application.Features.Listings.GetEndedListing;
using Imova.Contracts.Listings;
using Imova.Domain.Favorites;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Favorites.Alerts;

// One run of the saved-listing alert job (the Worker calls RunAsync every few minutes). For every
// favorite with something new to tell (see Favorite for what each one remembers):
// - its listing ended (EndedListingStatuses — what shows the 410 page) → one "no longer available"
//   email with the 410 page's summary, and nothing more is ever sent for that favorite;
// - its Active listing's price differs from the one the user knows → one email "old → new", at most
//   once per Favorite.PriceAlertInterval: further changes inside it wait, then go out together as
//   known price → latest price (a price that came back to the known one sends nothing).
// Only to confirmed addresses whose owner left these emails on (ApplicationUser.EmailFavoriteUpdates),
// and never to the listing's own owner; for everyone else the favorite catches up silently, so
// turning the emails back on doesn't send old news. A failed send records nothing → retried next run.
// A listing in review, suspended etc. is skipped until it is Active or ended again.
public class FavoriteAlerts(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    IEmailSender emailSender,
    AppOptions appOptions,
    FavoriteAlertUnsubscribeTokens unsubscribeTokens,
    TimeProvider timeProvider,
    ILogger<FavoriteAlerts> logger) : IScheduledJob
{
    // The rest waits for the next run (ordered by id; the ones handled here no longer match).
    public const int MaxPerRun = 500;

    // Returns how many emails were sent.
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var priceAlertAllowedBefore = now - Favorite.PriceAlertInterval;

        var candidates = await (
                from favorite in dbContext.Favorites
                where favorite.EndedAlertSentAt == null
                join listing in dbContext.Listings on favorite.ListingId equals listing.Id
                where EndedListingStatuses.All.Contains(listing.Status)
                      || (listing.Status == ListingStatus.Active
                          && (favorite.KnownPriceAmount == null
                              || favorite.KnownPriceCurrency == null
                              || ((favorite.KnownPriceAmount != listing.Price.Amount || favorite.KnownPriceCurrency != listing.Price.Currency)
                                  && (favorite.PriceAlertSentAt == null || favorite.PriceAlertSentAt <= priceAlertAllowedBefore))))
                orderby favorite.Id
                select new { Favorite = favorite, Listing = listing })
            .Take(MaxPerRun)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return 0;
        }

        var userIds = candidates.Select(c => c.Favorite.UserId).Distinct().ToList();
        var recipients = await dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id) && u.EmailConfirmed && u.EmailFavoriteUpdates && u.Email != null)
            .ToDictionaryAsync(u => u.Id, u => u.Email!, cancellationToken);

        var publisherIds = candidates.Select(c => c.Listing.PublisherId).Distinct().ToList();
        var ownerByPublisherId = await dbContext.Publishers.AsNoTracking()
            .Where(p => publisherIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.UserId, cancellationToken);

        var endedIds = candidates
            .Where(c => EndedListingStatuses.All.Contains(c.Listing.Status))
            .Select(c => c.Listing.Id)
            .Distinct()
            .ToList();
        var endedSummaries = await EndedListingSummaries.LoadAsync(dbContext, endedIds, cancellationToken);

        // For the listing card in each email: main photo and place names, the way the site's cards show them.
        var emailedListings = candidates
            .Where(c => recipients.ContainsKey(c.Favorite.UserId))
            .Select(c => c.Listing)
            .DistinctBy(l => l.Id)
            .ToList();
        var cardsById = (await ListingDtoLoader.LoadAsync(dbContext, blobStorageService, emailedListings, currentUserId: null, cancellationToken))
            .ToDictionary(l => l.Id, l => new CardDetails(
                (l.Photos.FirstOrDefault(p => p.IsPrimary) ?? l.Photos.OrderBy(p => p.SortOrder).FirstOrDefault())?.CardUrl,
                l.Property.Location is { } loc ? ListingEmailText.Location(loc.RaionName, loc.LocalitateName, loc.ChisinauSectorName) : null,
                l.Property.PropertyType,
                l.Property.TotalAreaM2));

        var sent = 0;
        foreach (var candidate in candidates)
        {
            var (favorite, listing) = (candidate.Favorite, candidate.Listing);
            var to = ownerByPublisherId.GetValueOrDefault(listing.PublisherId) == favorite.UserId
                ? null
                : recipients.GetValueOrDefault(favorite.UserId);

            if (EndedListingStatuses.All.Contains(listing.Status))
            {
                if (to is not null && endedSummaries.TryGetValue(listing.Id, out var ended))
                {
                    if (!await TrySendAsync(BuildEndedEmail(to, favorite.UserId, ended, cardsById.GetValueOrDefault(listing.Id)), favorite, cancellationToken))
                    {
                        continue;
                    }

                    sent++;
                }

                favorite.RecordEnded(now);
            }
            else if (!favorite.KnowsPrice)
            {
                favorite.RecordPrice(listing.Price, now, emailSent: false);
            }
            else if (favorite.IsPriceAlertDue(listing.Price, now))
            {
                if (to is not null)
                {
                    if (!await TrySendAsync(BuildPriceEmail(to, favorite, listing, cardsById.GetValueOrDefault(listing.Id)), favorite, cancellationToken))
                    {
                        continue;
                    }

                    sent++;
                }

                favorite.RecordPrice(listing.Price, now, emailSent: to is not null);
            }
            else
            {
                continue;
            }

            // Per favorite, so a failure later in the run doesn't re-send the ones already emailed.
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return sent;
    }

    private async Task<bool> TrySendAsync(EmailMessage email, Favorite favorite, CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendAsync(email, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Nothing recorded → the next run tries again.
            logger.LogWarning(ex, "Could not send the saved-listing alert for favorite {FavoriteId}.", favorite.Id);
            return false;
        }
    }

    private sealed record CardDetails(string? PhotoUrl, string? Location, string PropertyType, decimal AreaM2);

    private EmailMessage BuildPriceEmail(string to, Favorite favorite, Listing listing, CardDetails? card) =>
        FavoriteAlertEmails.PriceChanged(
            to,
            new FavoriteAlertEmails.PriceChange(
                listing.Id,
                listing.Title,
                listing.TransactionType.ToString(),
                favorite.KnownPriceAmount!.Value,
                favorite.KnownPriceCurrency!.Value.ToString(),
                listing.Price.Amount,
                listing.Price.Currency.ToString(),
                card?.PropertyType ?? string.Empty,
                card?.AreaM2 ?? 0,
                card?.Location,
                card?.PhotoUrl),
            listingUrl: appOptions.WebUrl($"/property/{listing.Id}"),
            UnsubscribeUrl(favorite.UserId),
            SettingsUrl);

    // The listing's own link now shows its 410 page: the summary, then "Anunțuri asemănătoare".
    private EmailMessage BuildEndedEmail(string to, Guid userId, EndedListingDto ended, CardDetails? card) =>
        FavoriteAlertEmails.Ended(
            to,
            ended,
            listingUrl: appOptions.WebUrl($"/property/{ended.Id}"),
            similarUrl: appOptions.WebUrl($"/property/{ended.Id}#similar-listings-title"),
            searchUrl: appOptions.WebUrl(
                $"/search?transactionType={ended.TransactionType}&propertyType={ended.PropertyType}&raionId={ended.RaionId}"),
            UnsubscribeUrl(userId),
            SettingsUrl,
            card?.PhotoUrl);

    private string UnsubscribeUrl(Guid userId) =>
        appOptions.WebUrl($"/favorites/unsubscribe?user={userId}&token={Uri.EscapeDataString(unsubscribeTokens.Create(userId))}");

    private string SettingsUrl => appOptions.WebUrl("/account?tab=notifications");
}
