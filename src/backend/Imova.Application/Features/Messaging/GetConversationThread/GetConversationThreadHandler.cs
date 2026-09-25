using Imova.Application.Common.Interfaces;
using Imova.Contracts.Messaging;
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
            page.Take(pageSize).OrderBy(m => m.CreatedAt).Select(m => m.ToDto(blobStorageService)).ToList(),
            hasMore,
            BlockedByMe: await MessagingAccess.IsBlockedAsync(dbContext, request.UserId, otherUserId, cancellationToken),
            BlockedByOther: await MessagingAccess.IsBlockedAsync(dbContext, otherUserId, request.UserId, cancellationToken));
    }
}
