using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Application.Features.Agencies.Logos;
using Imova.Application.Features.Agencies.Members;
using Imova.Application.Features.Listings;
using Imova.Domain.Agencies;
using Imova.Application.Features.Media.Sizes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Account;

// Erases an account for good (the right to erasure — GDPR art. 17, Moldova's Law 133/2011): it is
// immediate and can't be undone. Callers have already checked it's really the owner asking (the
// password, or the emailed link — see DeleteAccount / ConfirmAccountDeletion).
//
// What goes, in one SaveChanges (so either all of it or none of it):
// - the account row itself, and with it (database cascade) its roles, external logins, refresh
//   tokens, favorites and saved searches — removed explicitly too, for providers without cascades;
// - its publisher and its private listings, with the listings' photos, favorites and (when nothing
//   else uses it) the property and its location — see ListingRemoval;
// - agencies it is the only member of, with their listings; from any other agency it leaves, and
//   the agency keeps its listings, handed to an Owner or Admin who stays (AgencyListingReassignment).
//   Refused while it's the last Owner of an agency with other members (EnsureCanDeleteAsync);
// - photos uploaded for a listing that was never created, and blocks in either direction;
// - conversations whose other participant is already gone too (nobody can read them any more).
// Then, best-effort, the files: listing photos, profile pictures, agency logos, message images
// nobody can see any more, and uploads that were never sent.
//
// What stays: messages the user sent in a conversation with someone who still has an account —
// they're part of *that* person's correspondence too (the thread shows "deleted account" and can't
// be continued; MessagingAccess) — and reports the user filed (moderation records about someone
// else). Neither carries the user's name, email or phone any more once the account row is gone.
public class AccountDeletion(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    AccountDeletionEmails emails,
    ILogger<AccountDeletion> logger)
{
    // The purpose Identity's token provider signs the emailed deletion link with.
    public const string TokenPurpose = "DeleteAccount";

    // False when the account doesn't exist (already deleted).
    public async Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        var (email, displayName, emailConfirmed) = (user.Email, user.DisplayName, user.EmailConfirmed);
        var publicBlobs = new HashSet<string>(StringComparer.Ordinal);
        var attachmentBlobs = new HashSet<string>(StringComparer.Ordinal);

        // Agencies first: refused outright while they're the last Owner of one that has other people
        // (nothing is touched). An agency they're alone in goes with them; from any other they leave,
        // and the agency keeps their listings — handed to an Owner or Admin who stays.
        var agencies = await AgenciesOfAsync(userId, cancellationToken);
        EnsureNotLastOwner(userId, agencies);

        var soleMemberAgencies = agencies.Where(a => a.Members.Count == 1).ToList();
        var now = DateTimeOffset.UtcNow;
        foreach (var agency in agencies.Except(soleMemberAgencies))
        {
            await AgencyListingReassignment.MoveAsync(
                dbContext, agency, userId, AgencyListingReassignment.DefaultHeir(agency, userId, actorId: null), cancellationToken);
            agency.RemoveMember(userId, now);
        }

        // Their own listings: the private ones, and those of the agencies that go with them.
        var soleAgencyIds = soleMemberAgencies.Select(a => (Guid?)a.Id).ToList();
        var publishers = await dbContext.Publishers.Where(p => p.UserId == userId).ToListAsync(cancellationToken);
        var publisherIds = publishers.Select(p => p.Id).ToList();
        var listings = await dbContext.Listings
            .Where(l => publisherIds.Contains(l.PublisherId) && (l.AgencyId == null || soleAgencyIds.Contains(l.AgencyId)))
            .ToListAsync(cancellationToken);
        publicBlobs.UnionWith(await ListingRemoval.RemoveAsync(dbContext, listings, cancellationToken));
        dbContext.Publishers.RemoveRange(publishers);

        publicBlobs.UnionWith(soleMemberAgencies.Select(a => a.LogoBlobName).OfType<string>().SelectMany(AgencyLogo.AllBlobNames));
        dbContext.Agencies.RemoveRange(soleMemberAgencies);

        // Photos uploaded under a listing id that never became a listing (see Photo / MediaAccess).
        var listingIds = listings.Select(l => l.Id).ToList();
        var strayPhotos = await dbContext.Photos
            .Where(p => p.UploadedByUserId == userId && !listingIds.Contains(p.ListingId))
            .Where(p => !dbContext.Listings.Any(l => l.Id == p.ListingId))
            .ToListAsync(cancellationToken);
        dbContext.Photos.RemoveRange(strayPhotos);
        publicBlobs.UnionWith(strayPhotos.SelectMany(PhotoSizes.AllBlobNames));

        dbContext.Favorites.RemoveRange(await dbContext.Favorites.Where(f => f.UserId == userId).ToListAsync(cancellationToken));
        dbContext.SavedSearches.RemoveRange(await dbContext.SavedSearches.Where(s => s.UserId == userId).ToListAsync(cancellationToken));
        dbContext.RefreshTokens.RemoveRange(await dbContext.RefreshTokens.Where(t => t.UserId == userId).ToListAsync(cancellationToken));
        dbContext.UserBlocks.RemoveRange(await dbContext.UserBlocks
            .Where(b => b.BlockerUserId == userId || b.BlockedUserId == userId)
            .ToListAsync(cancellationToken));

        var keptAttachmentBlobs = await RemoveOrphanedConversationsAsync(userId, attachmentBlobs, cancellationToken);

        // Profile pictures: the current one, and any earlier upload left under the user's prefix.
        if (user.ProfilePictureUrl is not null)
        {
            AddOwnBlob(publicBlobs, user.ProfilePictureUrl);
        }

        publicBlobs.UnionWith(await blobStorageService.ListBlobNamesAsync($"profile-pictures/{userId}/", cancellationToken));

        // Message images uploaded but never sent — anything under the user's prefix that no
        // remaining message uses.
        attachmentBlobs.UnionWith(
            (await blobStorageService.ListMessageAttachmentBlobNamesAsync($"messages/{userId}/", cancellationToken))
            .Where(name => !keptAttachmentBlobs.Contains(name)));

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Only the id: the log mustn't become where the user's details live on.
        logger.LogInformation("Account {UserId} deleted at the owner's request.", userId);

        await DeleteFilesAsync(userId, publicBlobs, attachmentBlobs, cancellationToken);

        if (emailConfirmed && !string.IsNullOrWhiteSpace(email))
        {
            try
            {
                await emails.SendDeletedAsync(email, displayName, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not send the account-deleted email for account {UserId}.", userId);
            }
        }

        return true;
    }

    // 400 account.lastAgencyOwner while the account is the only Owner of an agency that has other
    // members — someone else must be made an Owner first. Checked before anything is deleted (and
    // before a deletion link is emailed).
    public async Task EnsureCanDeleteAsync(Guid userId, CancellationToken cancellationToken) =>
        EnsureNotLastOwner(userId, await AgenciesOfAsync(userId, cancellationToken));

    private Task<List<Agency>> AgenciesOfAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Agencies.Include(a => a.Members)
            .Where(a => a.Members.Any(m => m.UserId == userId))
            .ToListAsync(cancellationToken);

    private static void EnsureNotLastOwner(Guid userId, IEnumerable<Agency> agencies)
    {
        var blocking = agencies
            .Where(a => a.Members.Count > 1 && a.RoleOf(userId) is AgencyRole.Owner && a.OwnerCount == 1)
            .Select(a => a.Name)
            .ToList();
        if (blocking.Count > 0)
        {
            throw new ValidationException(
            [
                CodedFailure.Of(
                    "Account",
                    $"You are the only owner of {string.Join(", ", blocking)}. Make another member an owner before deleting your account.",
                    ErrorCodes.AccountLastAgencyOwner,
                    CodedFailure.Params(("agencies", string.Join(", ", blocking)))),
            ]);
        }
    }

    // Conversations where the other side's account is gone too are removed (their messages,
    // images and reports with them). Returns the attachment blobs the remaining conversations
    // still show.
    private async Task<HashSet<string>> RemoveOrphanedConversationsAsync(
        Guid userId, HashSet<string> attachmentBlobs, CancellationToken cancellationToken)
    {
        var conversations = await dbContext.Conversations
            .Where(c => c.InitiatorUserId == userId || c.PublisherUserId == userId)
            .ToListAsync(cancellationToken);
        var otherUserIds = conversations.Select(c => c.OtherParticipant(userId)).Distinct().ToList();
        var existing = (await dbContext.Users.Where(u => otherUserIds.Contains(u.Id)).Select(u => u.Id).ToListAsync(cancellationToken))
            .ToHashSet();

        var orphaned = conversations.Where(c => !existing.Contains(c.OtherParticipant(userId))).ToList();
        var orphanedIds = orphaned.Select(c => c.Id).ToList();
        var keptIds = conversations.Select(c => c.Id).Except(orphanedIds).ToList();

        var orphanedMessages = await dbContext.Messages
            .Include(m => m.Attachments)
            .Where(m => orphanedIds.Contains(m.ConversationId))
            .ToListAsync(cancellationToken);
        attachmentBlobs.UnionWith(orphanedMessages.SelectMany(m => m.Attachments).Select(a => a.BlobName));
        dbContext.Messages.RemoveRange(orphanedMessages);
        dbContext.ConversationReports.RemoveRange(await dbContext.ConversationReports
            .Where(r => orphanedIds.Contains(r.ConversationId))
            .ToListAsync(cancellationToken));
        dbContext.Conversations.RemoveRange(orphaned);

        var kept = await dbContext.Messages
            .AsNoTracking()
            .Include(m => m.Attachments)
            .Where(m => keptIds.Contains(m.ConversationId) && m.SenderUserId == userId)
            .ToListAsync(cancellationToken);
        return kept.SelectMany(m => m.Attachments).Select(a => a.BlobName).ToHashSet(StringComparer.Ordinal);
    }

    // The account is already gone by now; a file that couldn't be deleted is only logged.
    private async Task DeleteFilesAsync(
        Guid userId, IEnumerable<string> publicBlobs, IEnumerable<string> attachmentBlobs, CancellationToken cancellationToken)
    {
        foreach (var blobName in publicBlobs)
        {
            try
            {
                await blobStorageService.DeleteAsync(blobName, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not delete file {BlobName} of deleted account {UserId}.", blobName, userId);
            }
        }

        foreach (var blobName in attachmentBlobs)
        {
            try
            {
                await blobStorageService.DeleteMessageAttachmentAsync(blobName, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not delete message image {BlobName} of deleted account {UserId}.", blobName, userId);
            }
        }
    }

    // Only our own blobs — a URL pointing anywhere else isn't ours to delete.
    private void AddOwnBlob(HashSet<string> blobs, string url)
    {
        if (blobStorageService.TryGetBlobNameFromUrl(url) is { } blobName)
        {
            blobs.Add(blobName);
        }
    }
}
