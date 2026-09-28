using Imova.Application.Common.Interfaces;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings;

// A listing only enters the admin review queue once its owner has confirmed their email — so an
// unverified (possibly throwaway) address can't put listings in front of moderators. Creating a
// listing still works unconfirmed: it just waits as a Draft, and confirming the email later sends
// every waiting Draft to review (SubmitWaitingDraftsAsync) without the owner doing anything else.
public static class ReviewEligibility
{
    public const string EmailNotConfirmedMessage = "Confirm your email address before sending a listing for review.";

    public static Task<bool> IsEmailConfirmedAsync(IApplicationDbContext dbContext, Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(u => u.Id == userId && u.EmailConfirmed, cancellationToken);

    // The owner is the user behind the listing's publisher — which may not be the caller (an admin).
    public static async Task<bool> IsOwnerEmailConfirmedAsync(
        IApplicationDbContext dbContext, Listing listing, CancellationToken cancellationToken)
    {
        var ownerId = await dbContext.Publishers
            .Where(p => p.Id == listing.PublisherId)
            .Select(p => (Guid?)p.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        return ownerId is { } id && await IsEmailConfirmedAsync(dbContext, id, cancellationToken);
    }

    // Call once the user's email has just been confirmed. Returns how many listings were submitted.
    public static async Task<int> SubmitWaitingDraftsAsync(IApplicationDbContext dbContext, Guid userId, CancellationToken cancellationToken)
    {
        var publisherIds = dbContext.Publishers.Where(p => p.UserId == userId).Select(p => p.Id);
        var drafts = await dbContext.Listings
            .Where(l => l.Status == ListingStatus.Draft && publisherIds.Contains(l.PublisherId))
            .ToListAsync(cancellationToken);

        foreach (var draft in drafts)
        {
            draft.SubmitForReview();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return drafts.Count;
    }
}
