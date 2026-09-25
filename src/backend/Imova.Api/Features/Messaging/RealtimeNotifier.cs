using Imova.Application.Common.Interfaces;
using Imova.Contracts.Messaging;
using Microsoft.AspNetCore.SignalR;

namespace Imova.Api.Features.Messaging;

public class RealtimeNotifier(IHubContext<MessagingHub> hubContext) : IRealtimeNotifier
{
    public Task MessageCreatedAsync(Guid recipientUserId, Guid senderUserId, MessageDto message, CancellationToken cancellationToken) =>
        hubContext.Clients
            .Groups([MessagingHub.UserGroup(recipientUserId), MessagingHub.UserGroup(senderUserId)])
            .SendAsync(RealtimeEvents.MessageReceived, message, cancellationToken);

    public Task MessageStatusChangedAsync(MessageStatusChangedDto change, Guid senderUserId, CancellationToken cancellationToken) =>
        hubContext.Clients.Group(MessagingHub.UserGroup(senderUserId))
            .SendAsync(RealtimeEvents.MessageStatusChanged, change, cancellationToken);

    public Task UnreadCountChangedAsync(Guid userId, int unreadCount, CancellationToken cancellationToken) =>
        hubContext.Clients.Group(MessagingHub.UserGroup(userId))
            .SendAsync(RealtimeEvents.UnreadCountChanged, new UnreadCountDto(unreadCount), cancellationToken);
}
