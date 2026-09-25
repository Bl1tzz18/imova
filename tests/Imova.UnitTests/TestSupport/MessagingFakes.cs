using Imova.Application.Common.Interfaces;
using Imova.Contracts.Messaging;

namespace Imova.UnitTests.TestSupport;

internal sealed class FakeRealtimeNotifier : IRealtimeNotifier
{
    public List<(Guid RecipientUserId, Guid SenderUserId, MessageDto Message)> Created { get; } = [];

    public List<(MessageStatusChangedDto Change, Guid SenderUserId)> StatusChanges { get; } = [];

    public List<(Guid UserId, int Count)> UnreadCounts { get; } = [];

    public Task MessageCreatedAsync(Guid recipientUserId, Guid senderUserId, MessageDto message, CancellationToken cancellationToken)
    {
        Created.Add((recipientUserId, senderUserId, message));
        return Task.CompletedTask;
    }

    public Task MessageStatusChangedAsync(MessageStatusChangedDto change, Guid senderUserId, CancellationToken cancellationToken)
    {
        StatusChanges.Add((change, senderUserId));
        return Task.CompletedTask;
    }

    public Task UnreadCountChangedAsync(Guid userId, int unreadCount, CancellationToken cancellationToken)
    {
        UnreadCounts.Add((userId, unreadCount));
        return Task.CompletedTask;
    }
}

internal sealed class FakePresenceTracker : IPresenceTracker
{
    public HashSet<Guid> Online { get; } = [];

    public bool IsOnline(Guid userId) => Online.Contains(userId);
}

internal sealed class FakeEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }
}

// A clock the test moves by hand.
internal sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}
