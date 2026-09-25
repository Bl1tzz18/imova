using Imova.Domain.Common;

namespace Imova.Domain.Messaging;

// A private thread about one Listing between exactly two accounts: the visitor who started it
// (Initiator) and the user behind the listing's Publisher. Never the listing's "Other" contact
// person — that one has no account; in-app messages always reach the publisher (see
// StartConversationHandler). At most one conversation per (listing, initiator): starting again
// reuses it.
public sealed class Conversation : AggregateRoot
{
    // How long after a notification email another one is held back for the same recipient.
    public static readonly TimeSpan EmailNotificationCooldown = TimeSpan.FromMinutes(10);

    // For EF Core materialization only.
    private Conversation()
        : base(Guid.Empty)
    {
    }

    private Conversation(Guid id, Guid listingId, Guid initiatorUserId, Guid publisherUserId, DateTimeOffset now)
        : base(id)
    {
        ListingId = listingId;
        InitiatorUserId = initiatorUserId;
        PublisherUserId = publisherUserId;
        CreatedAt = now;
        LastMessageAt = now;
    }

    public Guid ListingId { get; private set; }

    public Guid InitiatorUserId { get; private set; }

    // The user who owns the listing's Publisher when the conversation started.
    public Guid PublisherUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    // Inbox sort key.
    public DateTimeOffset LastMessageAt { get; private set; }

    // Archiving is per participant — it only hides the conversation from that person's inbox.
    public bool ArchivedByInitiator { get; private set; }

    public bool ArchivedByPublisher { get; private set; }

    // Per recipient, for the notification-email cooldown (see ShouldEmail).
    public DateTimeOffset? InitiatorNotifiedAt { get; private set; }

    public DateTimeOffset? PublisherNotifiedAt { get; private set; }

    public static Conversation Start(Guid listingId, Guid initiatorUserId, Guid publisherUserId, DateTimeOffset now)
    {
        if (listingId == Guid.Empty)
        {
            throw new ArgumentException("ListingId is required.", nameof(listingId));
        }

        if (initiatorUserId == Guid.Empty || publisherUserId == Guid.Empty)
        {
            throw new ArgumentException("Both participants are required.");
        }

        if (initiatorUserId == publisherUserId)
        {
            throw new InvalidOperationException("You can't start a conversation about your own listing.");
        }

        return new Conversation(Guid.NewGuid(), listingId, initiatorUserId, publisherUserId, now);
    }

    public bool IsParticipant(Guid userId) => userId == InitiatorUserId || userId == PublisherUserId;

    public Guid OtherParticipant(Guid userId)
    {
        EnsureParticipant(userId);
        return userId == InitiatorUserId ? PublisherUserId : InitiatorUserId;
    }

    public bool IsArchivedFor(Guid userId)
    {
        EnsureParticipant(userId);
        return userId == InitiatorUserId ? ArchivedByInitiator : ArchivedByPublisher;
    }

    public void Archive(Guid userId) => SetArchived(userId, true);

    public void Unarchive(Guid userId) => SetArchived(userId, false);

    // A new message brings the conversation back into both inboxes.
    public Message AddMessage(
        Guid senderUserId, string body, IEnumerable<MessageAttachment> attachments, bool isFlagged, string? flagReason, DateTimeOffset now)
    {
        EnsureParticipant(senderUserId);
        var message = Message.Create(Id, senderUserId, body, attachments, isFlagged, flagReason, now);
        LastMessageAt = now;
        ArchivedByInitiator = false;
        ArchivedByPublisher = false;
        return message;
    }

    // Email the recipient about a new message only when it's the first unread one in this
    // conversation and nothing was emailed to them for it within the cooldown — a burst of
    // messages yields one email.
    public bool ShouldEmail(Guid recipientUserId, int unreadBeforeThisMessage, DateTimeOffset now)
    {
        if (unreadBeforeThisMessage > 0)
        {
            return false;
        }

        var lastNotified = LastNotifiedAt(recipientUserId);
        return lastNotified is null || now - lastNotified.Value >= EmailNotificationCooldown;
    }

    public void RecordEmailSent(Guid recipientUserId, DateTimeOffset now)
    {
        EnsureParticipant(recipientUserId);
        if (recipientUserId == InitiatorUserId)
        {
            InitiatorNotifiedAt = now;
        }
        else
        {
            PublisherNotifiedAt = now;
        }
    }

    private DateTimeOffset? LastNotifiedAt(Guid userId)
    {
        EnsureParticipant(userId);
        return userId == InitiatorUserId ? InitiatorNotifiedAt : PublisherNotifiedAt;
    }

    private void SetArchived(Guid userId, bool archived)
    {
        EnsureParticipant(userId);
        if (userId == InitiatorUserId)
        {
            ArchivedByInitiator = archived;
        }
        else
        {
            ArchivedByPublisher = archived;
        }
    }

    private void EnsureParticipant(Guid userId)
    {
        if (!IsParticipant(userId))
        {
            throw new InvalidOperationException("That user is not part of this conversation.");
        }
    }
}
