using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Application.Features.Listings.GetEndedListing;
using Imova.Application.Features.Messaging;
using Imova.Domain.Listings;
using Imova.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Retention;

// One run of the Worker's data-retention job — the limits the privacy policy (/privacy) promises:
// - ended listings (sold, rented, expired, archived) untouched for Listing.EndedKeepFor are deleted
//   with everything ListingRemoval takes (photos and their sizes, favorites, reports, price history);
// - conversations with no message for Conversation.InactiveKeepFor are deleted with their messages,
//   images (and their display copies) and closed reports — one with an open report waits for an admin;
// - the IP address on admin audit entries is cleared after AdminAuditEntry.IpKeepFor (the entry stays).
// Rows first, files after (best-effort, logged), as everywhere else.
public class DataRetentionCleanup(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    TimeProvider timeProvider,
    ILogger<DataRetentionCleanup> logger) : IScheduledJob
{
    public const int BatchSize = 100;

    // Returns how many records were deleted or cleared.
    public async Task<int> RunAsync(CancellationToken cancellationToken) =>
        await DeleteEndedListingsAsync(cancellationToken)
        + await DeleteInactiveConversationsAsync(cancellationToken)
        + await ClearOldAuditIpsAsync(cancellationToken);

    private async Task<int> DeleteEndedListingsAsync(CancellationToken cancellationToken)
    {
        var before = timeProvider.GetUtcNow() - Listing.EndedKeepFor;
        var deleted = 0;
        while (true)
        {
            var batch = await dbContext.Listings
                .Where(l => EndedListingStatuses.All.Contains(l.Status) && l.UpdatedAt < before)
                .OrderBy(l => l.UpdatedAt)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                return deleted;
            }

            var blobs = await ListingRemoval.RemoveAsync(dbContext, batch, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            deleted += batch.Count;

            foreach (var blobName in blobs)
            {
                await TryDeleteAsync(blobName, isMessageImage: false, cancellationToken);
            }
        }
    }

    private async Task<int> DeleteInactiveConversationsAsync(CancellationToken cancellationToken)
    {
        var before = timeProvider.GetUtcNow() - Conversation.InactiveKeepFor;
        var deleted = 0;
        while (true)
        {
            var batch = await dbContext.Conversations
                .Where(c => c.LastMessageAt < before
                            && !dbContext.ConversationReports.Any(r => r.ConversationId == c.Id && r.ResolvedAt == null))
                .OrderBy(c => c.LastMessageAt)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                return deleted;
            }

            var ids = batch.Select(c => c.Id).ToList();
            var messages = await dbContext.Messages
                .Include(m => m.Attachments)
                .Where(m => ids.Contains(m.ConversationId))
                .ToListAsync(cancellationToken);
            var blobs = messages.SelectMany(m => m.Attachments).SelectMany(a => MessageImageSizes.AllBlobNames(a.BlobName)).ToList();

            dbContext.Messages.RemoveRange(messages);
            dbContext.ConversationReports.RemoveRange(
                await dbContext.ConversationReports.Where(r => ids.Contains(r.ConversationId)).ToListAsync(cancellationToken));
            dbContext.Conversations.RemoveRange(batch);
            await dbContext.SaveChangesAsync(cancellationToken);
            deleted += batch.Count;

            foreach (var blobName in blobs)
            {
                await TryDeleteAsync(blobName, isMessageImage: true, cancellationToken);
            }
        }
    }

    private async Task<int> ClearOldAuditIpsAsync(CancellationToken cancellationToken)
    {
        var before = timeProvider.GetUtcNow() - AdminAuditEntry.IpKeepFor;
        var entries = await dbContext.AdminAuditEntries
            .Where(e => e.IpAddress != null && e.CreatedAt < before)
            .ToListAsync(cancellationToken);
        foreach (var entry in entries)
        {
            entry.IpAddress = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return entries.Count;
    }

    private async Task TryDeleteAsync(string blobName, bool isMessageImage, CancellationToken cancellationToken)
    {
        try
        {
            if (isMessageImage)
            {
                await blobStorageService.DeleteMessageAttachmentAsync(blobName, cancellationToken);
            }
            else
            {
                await blobStorageService.DeleteAsync(blobName, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not delete file {BlobName} past its retention period.", blobName);
        }
    }
}
