using Imova.Application.Common.Interfaces;
using Imova.Contracts.Messaging;
using Imova.Domain.Messaging;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Messaging;

// Tells each sender which of their messages just became Delivered/Read (best-effort).
public static class MessageStatusNotifications
{
    public static async Task NotifySendersAsync(
        IRealtimeNotifier realtimeNotifier,
        ILogger logger,
        IEnumerable<Message> changed,
        MessageStatus status,
        CancellationToken cancellationToken)
    {
        foreach (var group in changed.GroupBy(m => (m.ConversationId, m.SenderUserId)))
        {
            try
            {
                await realtimeNotifier.MessageStatusChangedAsync(
                    new MessageStatusChangedDto(group.Key.ConversationId, group.Select(m => m.Id).ToList(), status.ToString()),
                    group.Key.SenderUserId,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not push a message status change.");
            }
        }
    }
}
