using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Application.Features.Listings.SearchListings;
using Imova.Domain.SavedSearches;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.SavedSearches.Alerts;

// One run of the saved-search alert job (the Worker calls RunAsync every few minutes): for every
// search with alerts on that is due (SavedSearch.IsAlertDue), email the listings published since
// its last run. Each listing is emailed at most once per search: a search's LastAlertedUpTo moves
// forward after each run, and a failed email leaves it where it was so the next run retries.
// Only confirmed email addresses get alerts.
public class SavedSearchAlerts(
    IApplicationDbContext dbContext,
    IListingSearch listingSearch,
    IBlobStorageService blobStorageService,
    IEmailSender emailSender,
    AppOptions appOptions,
    SavedSearchUnsubscribeTokens unsubscribeTokens,
    TimeProvider timeProvider,
    ILogger<SavedSearchAlerts> logger) : IScheduledJob
{
    public const int MaxListingsPerEmail = 10;

    // Listings approved in the last few seconds may not be committed yet when this run searches —
    // leave them to the next run instead of risking skipping them.
    public static readonly TimeSpan SettleDelay = TimeSpan.FromSeconds(30);

    // Returns how many alert emails were sent.
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var upTo = now - SettleDelay;

        var candidates = await (
                from savedSearch in dbContext.SavedSearches
                where savedSearch.AlertFrequency != AlertFrequency.Off
                join user in dbContext.Users on savedSearch.UserId equals user.Id
                where user.EmailConfirmed && user.Email != null
                select new { SavedSearch = savedSearch, Email = user.Email! })
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var candidate in candidates.Where(c => c.SavedSearch.IsAlertDue(now)))
        {
            var savedSearch = candidate.SavedSearch;
            var emailSent = false;

            if (SavedSearchMatches.ToQuery(savedSearch) is { } query)
            {
                var (ids, total) = await listingSearch.SearchAsync(
                    query with { PublishedAfter = savedSearch.LastAlertedUpTo, PublishedBefore = upTo, PageSize = MaxListingsPerEmail },
                    cancellationToken);

                if (total > 0)
                {
                    try
                    {
                        await emailSender.SendAsync(await BuildEmailAsync(candidate.Email, savedSearch, ids, total, cancellationToken), cancellationToken);
                        emailSent = true;
                        sent++;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Not recorded → the same listings are retried next run.
                        logger.LogWarning(ex, "Could not send the alert email for saved search {SavedSearchId}.", savedSearch.Id);
                        continue;
                    }
                }
            }

            savedSearch.RecordAlertRun(upTo, emailSent, now);

            // Per search, so a failure later in the run doesn't re-send the ones already emailed.
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return sent;
    }

    private async Task<EmailMessage> BuildEmailAsync(
        string to, SavedSearch savedSearch, IReadOnlyList<Guid> ids, int total, CancellationToken cancellationToken)
    {
        // The same data the site's cards show (main photo URL, location names), via the shared loader.
        var byId = await dbContext.Listings.AsNoTracking()
            .Where(l => ids.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, cancellationToken);
        // In the search's order; a listing removed in the meantime is simply left out.
        var ordered = ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        var dtos = await ListingDtoLoader.LoadAsync(dbContext, blobStorageService, ordered, currentUserId: null, cancellationToken);

        var listings = dtos.Select(l =>
        {
            var photo = l.Photos.FirstOrDefault(p => p.IsPrimary) ?? l.Photos.OrderBy(p => p.SortOrder).FirstOrDefault();
            var location = l.Property.Location is { } loc
                ? string.Join(", ", new[] { loc.ChisinauSectorName ?? loc.LocalitateName, loc.RaionName }.Where(n => !string.IsNullOrWhiteSpace(n)))
                : null;
            return new AlertListing(
                l.Id,
                l.Title,
                l.Price.Amount,
                l.Price.Currency,
                l.TransactionType,
                l.Property.PropertyType,
                l.Property.TotalAreaM2,
                string.IsNullOrWhiteSpace(location) ? null : location,
                photo?.CardUrl,
                appOptions.WebUrl($"/property/{l.Id}"));
        }).ToList();

        return SavedSearchAlertEmail.Build(
            to,
            savedSearch.Name,
            listings,
            total,
            openUrl: appOptions.WebUrl($"/saved-searches/{savedSearch.Id}/open"),
            unsubscribeUrl: appOptions.WebUrl(
                $"/saved-searches/unsubscribe?id={savedSearch.Id}&token={Uri.EscapeDataString(unsubscribeTokens.Create(savedSearch.Id))}"));
    }
}
