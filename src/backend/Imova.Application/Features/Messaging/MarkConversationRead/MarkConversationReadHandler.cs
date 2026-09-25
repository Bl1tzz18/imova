using Imova.Application.Common.Interfaces;
using Imova.Domain.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Messaging.MarkConversationRead;

public class MarkConversationReadHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotifier realtimeNotifier,
    TimeProvider timeProvider,
    ILogger<MarkConversationReadHandler> logger)
    : IRequestHandler<MarkConversationReadCommand, bool>
{
    public async Task<bool> Handle(MarkConversationReadCommand request, CancellationToken cancellationToken)
    {
        var conversation = await MessagingAccess.FindForParticipantAsync(dbContext, request.ConversationId, request.UserId, cancellationToken);
        if (conversation is null)
        {
            return false;
        }

        var unread = await dbContext.Messages
            .Where(m => m.ConversationId == conversation.Id && m.SenderUserId != request.UserId && m.ReadAt == null)
            .ToListAsync(cancellationToken);
        if (unread.Count == 0)
        {
            return true;
        }

        var now = timeProvider.GetUtcNow();
        unread.ForEach(m => m.MarkRead(now));
        await dbContext.SaveChangesAsync(cancellationToken);

        await MessageStatusNotifications.NotifySendersAsync(realtimeNotifier, logger, unread, MessageStatus.Read, cancellationToken);
        try
        {
            var remaining = await MessagingAccess.UnreadCountAsync(dbContext, request.UserId, cancellationToken);
            await realtimeNotifier.UnreadCountChangedAsync(request.UserId, remaining, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not push the unread count.");
        }

        return true;
    }
}
