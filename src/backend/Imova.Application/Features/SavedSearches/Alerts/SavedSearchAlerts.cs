using Imova.Application.Common;
using Imova.Application.Common.Emails;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Application.Features.Listings.SearchListings;
using Imova.Application.Features.Telegram;
using Imova.Domain.SavedSearches;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.SavedSearches.Alerts;

// One run of the saved-search alert job (the Worker calls RunAsync every minute): for every search
// with alerts on that is due (SavedSearch.IsAlertDue), send the listings published since its last
// run — by email and/or Telegram, as the search's AlertChannels say (Targets). Each listing is sent
// at most once per search: a search's LastAlertedUpTo moves forward after each run, and when every
// channel it tried failed it stays where it was so the next run retries. Emails only go to
// confirmed addresses; Telegram only to a connected chat while the Worker has a bot.
public class SavedSearchAlerts(
    IApplicationDbContext dbContext,
    IListingSearch listingSearch,
    IBlobStorageService blobStorageService,
    IEmailSender emailSender,
    SavedSearchTelegramAlert telegramAlert,
    TelegramOptions telegramOptions,
    AppOptions appOptions,
    SavedSearchUnsubscribeTokens unsubscribeTokens,
    TimeProvider timeProvider,
    ILogger<SavedSearchAlerts> logger) : IScheduledJob
{
    public const int MaxListingsPerEmail = 10;

    // Listings approved in the last few seconds may not be committed yet when this run searches —
    // leave them to the next run instead of risking skipping them.
    public static readonly TimeSpan SettleDelay = TimeSpan.FromSeconds(30);

    // Where one search's alert goes this run. A search set to Telegram whose account has no usable
    // chat (never connected, disconnected, blocked the bot) falls back to email rather than going silent.
    public static (bool Email, bool Telegram) Targets(AlertChannels channels, bool emailUsable, bool telegramUsable)
    {
        var email = channels.HasFlag(AlertChannels.Email) && emailUsable;
        var telegram = channels.HasFlag(AlertChannels.Telegram) && telegramUsable;
        if (!email && !telegram && channels.HasFlag(AlertChannels.Telegram) && emailUsable)
        {
            email = true;
        }

        return (email, telegram);
    }

    // Returns how many alerts were sent (an email and a Telegram alert for the same search count as two).
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var upTo = now - SettleDelay;
        var botRunning = !string.IsNullOrWhiteSpace(telegramOptions.BotToken);

        var candidates = await (
                from savedSearch in dbContext.SavedSearches
                where savedSearch.AlertFrequency != AlertFrequency.Off
                join user in dbContext.Users on savedSearch.UserId equals user.Id
                join link in dbContext.TelegramLinks on user.Id equals link.UserId into links
                from link in links.DefaultIfEmpty()
                where (user.EmailConfirmed && user.Email != null) || link != null
                select new
                {
                    SavedSearch = savedSearch,
                    Email = user.EmailConfirmed ? user.Email : null,
                    user.PreferredLanguage,
                    ChatId = link == null ? (long?)null : link.ChatId,
                })
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var candidate in candidates.Where(c => c.SavedSearch.IsAlertDue(now)))
        {
            var savedSearch = candidate.SavedSearch;
            var (byEmail, byTelegram) = Targets(savedSearch.AlertChannels, candidate.Email is not null, botRunning && candidate.ChatId is not null);
            if (!byEmail && !byTelegram)
            {
                // Nowhere to send it yet (e.g. an unconfirmed address): the matches wait, like before.
                continue;
            }

            var alertSent = false;
            if (SavedSearchMatches.ToQuery(savedSearch) is { } query)
            {
                var (ids, total) = await listingSearch.SearchAsync(
                    query with { PublishedAfter = savedSearch.LastAlertedUpTo, PublishedBefore = upTo, PageSize = MaxListingsPerEmail },
                    cancellationToken);

                if (total > 0)
                {
                    var listings = await LoadListingsAsync(ids, cancellationToken);
                    var failed = 0;

                    if (byTelegram)
                    {
                        try
                        {
                            if (await telegramAlert.SendAsync(candidate.ChatId!.Value, Languages.Normalize(candidate.PreferredLanguage), savedSearch, listings, total, cancellationToken))
                            {
                                alertSent = true;
                                sent++;
                            }
                        }
                        catch (TelegramChatUnavailableException)
                        {
                            // Blocked the bot: forget the chat; from the next run this search falls back to email.
                            failed++;
                            await DropChatAsync(candidate.ChatId!.Value, cancellationToken);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            failed++;
                            logger.LogWarning(ex, "Could not send the Telegram alert for saved search {SavedSearchId}.", savedSearch.Id);
                        }
                    }

                    if (byEmail)
                    {
                        try
                        {
                            await emailSender.SendAsync(BuildEmail(candidate.Email!, candidate.PreferredLanguage, savedSearch, listings, total), cancellationToken);
                            alertSent = true;
                            sent++;
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            failed++;
                            logger.LogWarning(ex, "Could not send the alert email for saved search {SavedSearchId}.", savedSearch.Id);
                        }
                    }

                    // Everything it tried failed → the same listings are retried next run. (One channel
                    // working is enough to move on, so it never repeats what already arrived.)
                    if (!alertSent && failed > 0)
                    {
                        continue;
                    }
                }
            }

            savedSearch.RecordAlertRun(upTo, alertSent, now);

            // Per search, so a failure later in the run doesn't re-send the ones already sent.
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return sent;
    }

    private async Task DropChatAsync(long chatId, CancellationToken cancellationToken)
    {
        var link = await dbContext.TelegramLinks.FirstOrDefaultAsync(l => l.ChatId == chatId, cancellationToken);
        if (link is not null)
        {
            dbContext.TelegramLinks.Remove(link);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    // The same data the site's cards show (main photo URL, location names), via the shared loader,
    // in the search's order; a listing removed in the meantime is simply left out.
    private async Task<List<AlertListing>> LoadListingsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        var byId = await dbContext.Listings.AsNoTracking()
            .Where(l => ids.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, cancellationToken);
        var ordered = ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        var dtos = await ListingDtoLoader.LoadAsync(dbContext, blobStorageService, ordered, currentUserId: null, cancellationToken);

        return dtos.Select(l =>
        {
            var photo = l.Photos.FirstOrDefault(p => p.IsPrimary) ?? l.Photos.OrderBy(p => p.SortOrder).FirstOrDefault();
            var location = l.Property.Location is { } loc
                ? ListingEmailText.Location(loc.RaionName, loc.LocalitateName, loc.ChisinauSectorName)
                : null;
            return new AlertListing(
                l.Id,
                l.Title,
                l.Price.Amount,
                l.Price.Currency,
                l.TransactionType,
                l.Property.PropertyType,
                l.Property.TotalAreaM2,
                location,
                photo?.CardUrl,
                appOptions.WebUrl($"/property/{l.Id}"));
        }).ToList();
    }

    private EmailMessage BuildEmail(string to, string language, SavedSearch savedSearch, IReadOnlyList<AlertListing> listings, int total)
    {
        return SavedSearchAlertEmail.Build(
            to,
            savedSearch.Name,
            listings,
            total,
            openUrl: appOptions.WebUrl($"/saved-searches/{savedSearch.Id}/open"),
            unsubscribeUrl: appOptions.WebUrl(
                $"/saved-searches/unsubscribe?id={savedSearch.Id}&token={Uri.EscapeDataString(unsubscribeTokens.Create(savedSearch.Id))}"),
            language);
    }
}
