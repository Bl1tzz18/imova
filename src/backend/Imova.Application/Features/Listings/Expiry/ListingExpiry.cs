using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Listings.Expiry;

// One run of the listing-expiry job (the Worker runs it periodically). An Active listing lives
// Listing.ActiveMonths from approval / re-publish / renewal (Listing.ExpiresAt):
// - ReminderBefore its end the owner gets one reminder email with a way to renew it — retried on
//   the next run if the email fails, never sent twice (Listing.ExpiryReminderSentAt);
// - once ExpiresAt has passed the listing becomes Expired (out of search) and the owner is told
//   they can re-activate it. That email is best-effort: the status change is what matters, and the
//   listing shows as expired in "Anunțurile mele" either way.
public class ListingExpiry(
    IApplicationDbContext dbContext,
    IEmailSender emailSender,
    AppOptions appOptions,
    TimeProvider timeProvider,
    ILogger<ListingExpiry> logger) : IScheduledJob
{
    public static readonly TimeSpan ReminderBefore = TimeSpan.FromDays(7);

    // Per run, per kind — a backlog is worked through over the next runs.
    public const int BatchSize = 200;

    // Returns how many listings were expired or reminded about.
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return await ExpireAsync(now, cancellationToken) + await RemindAsync(now, cancellationToken);
    }

    private async Task<int> ExpireAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var due = await WithOwnerEmailsAsync(
            dbContext.Listings.Where(l => l.Status == ListingStatus.Active && l.ExpiresAt != null && l.ExpiresAt <= now),
            cancellationToken);

        foreach (var (listing, email) in due)
        {
            listing.Expire();

            // Per listing, so an email failure below never undoes or repeats an expiry.
            await dbContext.SaveChangesAsync(cancellationToken);

            if (email is not null)
            {
                await TrySendAsync(ListingExpiryEmails.Expired(email, listing.Title, MyListingsUrl), listing.Id, cancellationToken);
            }
        }

        return due.Count;
    }

    private async Task<int> RemindAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var remindBy = now + ReminderBefore;
        var due = await WithOwnerEmailsAsync(
            dbContext.Listings.Where(l =>
                l.Status == ListingStatus.Active
                && l.ExpiryReminderSentAt == null
                && l.ExpiresAt != null && l.ExpiresAt > now && l.ExpiresAt <= remindBy),
            cancellationToken);

        var sent = 0;
        foreach (var (listing, email) in due)
        {
            if (email is not null)
            {
                if (!await TrySendAsync(ListingExpiryEmails.Reminder(email, listing.Title, listing.ExpiresAt!.Value, MyListingsUrl), listing.Id, cancellationToken))
                {
                    continue; // Retried next run.
                }

                sent++;
            }

            // Also when there's nobody to email (an account without an address): don't look again.
            listing.MarkExpiryReminderSent(now);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return sent;
    }

    private string MyListingsUrl => appOptions.WebUrl("/my-listings");

    // The owner is the user behind the listing's publisher (see ListingAccess).
    private async Task<List<(Listing Listing, string? Email)>> WithOwnerEmailsAsync(
        IQueryable<Listing> listings, CancellationToken cancellationToken)
    {
        var rows = await (
                from listing in listings
                join publisher in dbContext.Publishers on listing.PublisherId equals publisher.Id
                join user in dbContext.Users on publisher.UserId equals user.Id
                orderby listing.ExpiresAt
                select new { Listing = listing, user.Email })
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Listing, r.Email)).ToList();
    }

    private async Task<bool> TrySendAsync(EmailMessage email, Guid listingId, CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendAsync(email, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not send the expiry email for listing {ListingId}.", listingId);
            return false;
        }
    }
}
