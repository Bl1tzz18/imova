using Imova.Application.Common.Interfaces;
using Imova.Contracts.Messaging;
using Imova.Domain.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Messaging;

// Builds inbox rows for a set of conversations from one user's point of view, with one batched
// query per related table (listings, photos, publishers, users, last messages, unread counts).
public static class ConversationSummaries
{
    public static async Task<List<ConversationSummaryDto>> LoadAsync(
        IApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        Guid viewerUserId,
        IReadOnlyList<Conversation> conversations,
        CancellationToken cancellationToken)
    {
        if (conversations.Count == 0)
        {
            return [];
        }

        var ids = conversations.Select(c => c.Id).ToList();
        var listingIds = conversations.Select(c => c.ListingId).Distinct().ToList();

        var listings = await dbContext.Listings.AsNoTracking()
            .Where(l => listingIds.Contains(l.Id))
            .Select(l => new { l.Id, l.Title, l.PublisherId })
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var photos = (await dbContext.Photos.AsNoTracking()
                .Where(p => listingIds.Contains(p.ListingId) && p.IsPrimary)
                .Select(p => new { p.ListingId, p.BlobName })
                .ToListAsync(cancellationToken))
            .GroupBy(p => p.ListingId)
            .ToDictionary(g => g.Key, g => g.First().BlobName);

        var publisherIds = listings.Values.Select(l => l.PublisherId).Distinct().ToList();
        var publishers = await dbContext.Publishers.AsNoTracking()
            .Where(p => publisherIds.Contains(p.Id))
            .Select(p => new { p.Id, p.DisplayName, p.LogoUrl })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var userIds = conversations.SelectMany(c => new[] { c.InitiatorUserId, c.PublisherUserId }).Distinct().ToList();
        var users = await dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.Email, u.ProfilePictureUrl })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        // Each conversation's LastMessageAt is its newest message's CreatedAt.
        var lastMessages = (await (
                from m in dbContext.Messages.AsNoTracking().Include(x => x.Attachments)
                join c in dbContext.Conversations on m.ConversationId equals c.Id
                where ids.Contains(c.Id) && m.CreatedAt == c.LastMessageAt
                select m).ToListAsync(cancellationToken))
            .GroupBy(m => m.ConversationId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.Id).First());

        var unread = await dbContext.Messages.AsNoTracking()
            .Where(m => ids.Contains(m.ConversationId) && m.SenderUserId != viewerUserId && m.ReadAt == null)
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConversationId, x => x.Count, cancellationToken);

        return conversations.Select(c =>
        {
            var listing = listings.GetValueOrDefault(c.ListingId);
            var isInitiator = c.InitiatorUserId == viewerUserId;
            var otherUserId = c.OtherParticipant(viewerUserId);
            var otherUser = users.GetValueOrDefault(otherUserId);

            // The visitor sees who they wrote to as the listing's publisher (e.g. the agency);
            // the publisher sees the visitor's own account name.
            var publisher = listing is null ? null : publishers.GetValueOrDefault(listing.PublisherId);
            var other = isInitiator && publisher is not null
                ? new ConversationParticipantDto(otherUserId, publisher.DisplayName, publisher.LogoUrl ?? otherUser?.ProfilePictureUrl)
                : new ConversationParticipantDto(
                    otherUserId, otherUser?.DisplayName ?? otherUser?.Email ?? "—", otherUser?.ProfilePictureUrl);

            var photo = photos.GetValueOrDefault(c.ListingId);
            return new ConversationSummaryDto(
                c.Id,
                new ConversationListingDto(c.ListingId, listing?.Title, photo is null ? null : blobStorageService.GetPublicUrl(photo)),
                other,
                lastMessages.GetValueOrDefault(c.Id)?.ToDto(blobStorageService),
                unread.GetValueOrDefault(c.Id),
                c.LastMessageAt,
                c.IsArchivedFor(viewerUserId),
                isInitiator);
        }).ToList();
    }

    // Case- and diacritics-insensitive match on the other participant's name or the listing title.
    public static bool Matches(ConversationSummaryDto summary, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        var needle = MessageContentFilter.Fold(search.Trim());
        return MessageContentFilter.Fold(summary.OtherParticipant.DisplayName).Contains(needle)
            || (summary.Listing.Title is not null && MessageContentFilter.Fold(summary.Listing.Title).Contains(needle));
    }
}
