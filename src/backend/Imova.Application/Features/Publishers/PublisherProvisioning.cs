using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Publishers;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Publishers;

// Every user publishes as an Individual by default. That publisher is created at registration /
// first Google sign-in (and by the Property→Listing data migration for pre-existing users);
// EnsureIndividualAsync is the safety net for anything that slipped through, so callers never
// have to handle "this user has no publisher yet".
public static class PublisherProvisioning
{
    public static Publisher NewIndividualFor(ApplicationUser user)
    {
        var email = user.Email ?? user.UserName ?? string.Empty;
        return Publisher.CreateIndividual(user.Id, DisplayNameFor(user), user.PhoneNumber, email);
    }

    // Keeps the user's Individual publisher's public contact details in step with their account
    // after a profile/phone change. A no-op if they somehow have none yet.
    public static async Task SyncIndividualAsync(
        IApplicationDbContext dbContext, ApplicationUser user, CancellationToken cancellationToken)
    {
        var publisher = await dbContext.Publishers.FirstOrDefaultAsync(
            p => p.UserId == user.Id && p.PublisherType == PublisherType.Individual, cancellationToken);
        if (publisher is null)
        {
            return;
        }

        publisher.UpdateContactDetails(DisplayNameFor(user), user.PhoneNumber, user.Email ?? publisher.Email);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Adds (without saving) the user's Individual publisher if it doesn't exist yet, and returns it.
    public static async Task<Publisher> EnsureIndividualAsync(
        IApplicationDbContext dbContext, Guid userId, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Publishers.FirstOrDefaultAsync(
            p => p.UserId == userId && p.PublisherType == PublisherType.Individual, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} does not exist.");

        var publisher = NewIndividualFor(user);
        dbContext.Publishers.Add(publisher);
        return publisher;
    }

    // DisplayName is optional at registration; fall back to the email's local part rather than
    // publishing the full address as a name.
    private static string DisplayNameFor(ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            return user.DisplayName;
        }

        var email = user.Email ?? user.UserName ?? string.Empty;
        var at = email.IndexOf('@');
        return at > 0 ? email[..at] : email;
    }
}
