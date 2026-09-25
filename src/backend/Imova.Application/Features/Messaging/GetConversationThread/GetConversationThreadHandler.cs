using Imova.Application.Common.Interfaces;
using Imova.Contracts.Messaging;
using Imova.Domain.Listings;
using Imova.Domain.Properties.Attributes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Messaging.GetConversationThread;

public class GetConversationThreadHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetConversationThreadQuery, ConversationThreadDto?>
{
    public async Task<ConversationThreadDto?> Handle(GetConversationThreadQuery request, CancellationToken cancellationToken)
    {
        var conversation = await MessagingAccess.FindForParticipantAsync(dbContext, request.ConversationId, request.UserId, cancellationToken);
        if (conversation is null)
        {
            return null;
        }

        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var messages = dbContext.Messages.AsNoTracking().Include(m => m.Attachments).Where(m => m.ConversationId == conversation.Id);

        if (request.Before is { } beforeId)
        {
            var cursor = await dbContext.Messages.AsNoTracking()
                .Where(m => m.Id == beforeId && m.ConversationId == conversation.Id)
                .Select(m => new { m.CreatedAt })
                .FirstOrDefaultAsync(cancellationToken);
            if (cursor is not null)
            {
                messages = messages.Where(m => m.CreatedAt < cursor.CreatedAt);
            }
        }

        // One extra row tells whether anything older is left.
        var page = await messages.OrderByDescending(m => m.CreatedAt).Take(pageSize + 1).ToListAsync(cancellationToken);
        var hasMore = page.Count > pageSize;

        var otherUserId = conversation.OtherParticipant(request.UserId);
        var summary = (await ConversationSummaries.LoadAsync(dbContext, blobStorageService, request.UserId, [conversation], cancellationToken))[0];
        return new ConversationThreadDto(
            summary,
            page.Take(pageSize).OrderBy(m => m.CreatedAt).Select(m => m.ToDto()).ToList(),
            hasMore,
            BlockedByMe: await MessagingAccess.IsBlockedAsync(dbContext, request.UserId, otherUserId, cancellationToken),
            BlockedByOther: await MessagingAccess.IsBlockedAsync(dbContext, otherUserId, request.UserId, cancellationToken),
            Listing: await ListingDetailsAsync(conversation.ListingId, summary.Listing.PhotoUrl, cancellationToken));
    }

    private async Task<ConversationListingDetailsDto?> ListingDetailsAsync(
        Guid listingId, string? photoUrl, CancellationToken cancellationToken)
    {
        var found = await (
                from l in dbContext.Listings.AsNoTracking()
                join p in dbContext.Properties.AsNoTracking() on l.PropertyId equals p.Id
                where l.Id == listingId
                select new { Listing = l, Property = p })
            .FirstOrDefaultAsync(cancellationToken);
        if (found is null)
        {
            return null;
        }

        var (listing, property) = (found.Listing, found.Property);
        return new ConversationListingDetailsDto(
            listing.Id,
            listing.Title,
            photoUrl,
            property.PropertyType.ToString(),
            listing.TransactionType.ToString(),
            property.TotalAreaM2,
            property.TypeSpecificAttributes switch
            {
                ApartmentAttributes a => a.Rooms,
                HouseAttributes h => h.Rooms,
                _ => null,
            },
            listing.Price.Amount,
            listing.Price.Currency.ToString(),
            listing.Status == ListingStatus.Active);
    }
}
