using Imova.Contracts.Messaging;

namespace Imova.Application.Common.Interfaces;

// Pushes messaging events to users' open browser tabs (SignalR, implemented in Imova.Api). Every
// call is best-effort: a user with no connection simply picks the change up on their next load.
public interface IRealtimeNotifier
{
    // To both participants: the recipient's tabs show it, the sender's other tabs stay in sync.
    Task MessageCreatedAsync(Guid recipientUserId, Guid senderUserId, MessageDto message, CancellationToken cancellationToken);

    // To the sender, whose messages just became Delivered or Read.
    Task MessageStatusChangedAsync(MessageStatusChangedDto change, Guid senderUserId, CancellationToken cancellationToken);

    Task UnreadCountChangedAsync(Guid userId, int unreadCount, CancellationToken cancellationToken);
}

// Who currently has at least one live realtime connection (tracked in Imova.Api).
public interface IPresenceTracker
{
    bool IsOnline(Guid userId);
}
