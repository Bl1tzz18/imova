using Imova.Application.Common.Interfaces;
using Imova.Domain.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Messaging.MarkMessagesDelivered;

public class MarkMessagesDeliveredHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotifier realtimeNotifier,
    TimeProvider timeProvider,
    ILogger<MarkMessagesDeliveredHandler> logger)
    : IRequestHandler<MarkMessagesDeliveredCommand, int>
{
    public async Task<int> Handle(MarkMessagesDeliveredCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var pending = await (
            from m in dbContext.Messages
            join c in dbContext.Conversations on m.ConversationId equals c.Id
            where (c.InitiatorUserId == userId || c.PublisherUserId == userId) && m.SenderUserId != userId && m.DeliveredAt == null
            select m).ToListAsync(cancellationToken);
        if (pending.Count == 0)
        {
            return 0;
        }

        var now = timeProvider.GetUtcNow();
        pending.ForEach(m => m.MarkDelivered(now));
        await dbContext.SaveChangesAsync(cancellationToken);

        await MessageStatusNotifications.NotifySendersAsync(realtimeNotifier, logger, pending, MessageStatus.Delivered, cancellationToken);
        return pending.Count;
    }
}
