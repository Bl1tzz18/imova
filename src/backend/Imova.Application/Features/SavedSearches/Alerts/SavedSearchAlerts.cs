using System.Globalization;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
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
    IEmailSender emailSender,
    AppOptions appOptions,
    SavedSearchUnsubscribeTokens unsubscribeTokens,
    TimeProvider timeProvider,
    ILogger<SavedSearchAlerts> logger)
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
        var listings = await dbContext.Listings.AsNoTracking()
            .Where(l => ids.Contains(l.Id))
            .Select(l => new { l.Id, l.Title, l.Price.Amount, l.Price.Currency })
            .ToListAsync(cancellationToken);
        // In the search's order; a listing removed in the meantime is simply left out.
        var byId = listings.ToDictionary(l => l.Id);
        var ordered = ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList();

        var subject = total == 1
            ? $"Un anunț nou pentru „{savedSearch.Name}” — IMOVA"
            : $"{total} anunțuri noi pentru „{savedSearch.Name}” — IMOVA";

        var lines = ordered.Select(l =>
            $"• {l.Title} — {l.Amount.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ')} {l.Currency}\n  {appOptions.WebUrl($"/property/{l.Id}")}");
        var more = total > ordered.Count ? $"\n…și încă {total - ordered.Count}.\n" : string.Empty;

        var body =
            $"Au apărut anunțuri noi pentru căutarea ta salvată „{savedSearch.Name}”:\n\n" +
            string.Join("\n\n", lines) + "\n" + more + "\n" +
            $"Vezi toate rezultatele: {appOptions.WebUrl($"/saved-searches/{savedSearch.Id}/open")}\n\n" +
            "Nu mai vrei aceste emailuri? Oprește alertele pentru această căutare:\n" +
            $"{appOptions.WebUrl($"/saved-searches/unsubscribe?id={savedSearch.Id}&token={Uri.EscapeDataString(unsubscribeTokens.Create(savedSearch.Id))}")}\n";

        return new EmailMessage(to, subject, body);
    }
}
