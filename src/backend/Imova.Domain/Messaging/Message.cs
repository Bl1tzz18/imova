using Imova.Domain.Common;

namespace Imova.Domain.Messaging;

// One message in a Conversation. There is exactly one recipient (the other participant), so the
// Sent -> Delivered -> Read status lives on the message itself as two timestamps.
public sealed class Message : Entity
{
    public const int MaxBodyLength = 2000;
    public const int MaxAttachments = 5;

    private readonly List<MessageAttachment> _attachments = [];

    // For EF Core materialization only.
    private Message()
        : base(Guid.Empty)
    {
        Body = null!;
    }

    private Message(Guid id, Guid conversationId, Guid senderUserId, string body, bool isFlagged, string? flagReason, DateTimeOffset now)
        : base(id)
    {
        ConversationId = conversationId;
        SenderUserId = senderUserId;
        Body = body;
        IsFlagged = isFlagged;
        FlagReason = flagReason;
        CreatedAt = now;
    }

    public Guid ConversationId { get; private set; }

    public Guid SenderUserId { get; private set; }

    public string Body { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    // Reached the recipient's device (they were connected, or loaded their inbox/thread).
    public DateTimeOffset? DeliveredAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    // Matched the content filter — still delivered, but listed for admin review.
    public bool IsFlagged { get; private set; }

    public string? FlagReason { get; private set; }

    // An admin reviewed the flag (moves it from "active" to "resolved" in the admin view).
    public DateTimeOffset? FlagResolvedAt { get; private set; }

    public Guid? FlagResolvedByUserId { get; private set; }

    public IReadOnlyCollection<MessageAttachment> Attachments => _attachments.AsReadOnly();

    public MessageStatus Status =>
        ReadAt is not null ? MessageStatus.Read : DeliveredAt is not null ? MessageStatus.Delivered : MessageStatus.Sent;

    internal static Message Create(
        Guid conversationId,
        Guid senderUserId,
        string body,
        IEnumerable<MessageAttachment> attachments,
        bool isFlagged,
        string? flagReason,
        DateTimeOffset now)
    {
        body = (body ?? string.Empty).Trim();
        var attachmentList = attachments.ToList();

        if (body.Length == 0 && attachmentList.Count == 0)
        {
            throw new ArgumentException("A message needs text or at least one image.", nameof(body));
        }

        if (body.Length > MaxBodyLength)
        {
            throw new ArgumentException($"A message can be at most {MaxBodyLength} characters.", nameof(body));
        }

        if (attachmentList.Count > MaxAttachments)
        {
            throw new ArgumentException($"A message can have at most {MaxAttachments} images.", nameof(attachments));
        }

        var message = new Message(Guid.NewGuid(), conversationId, senderUserId, body, isFlagged, flagReason, now);
        for (var i = 0; i < attachmentList.Count; i++)
        {
            message._attachments.Add(attachmentList[i].For(message.Id, i));
        }

        return message;
    }

    // Resolving twice keeps the first admin and time; only a flagged message can be resolved.
    public void ResolveFlag(Guid adminUserId, DateTimeOffset now)
    {
        if (!IsFlagged)
        {
            throw new InvalidOperationException("Only a flagged message can be resolved.");
        }

        if (FlagResolvedAt is not null)
        {
            return;
        }

        FlagResolvedAt = now;
        FlagResolvedByUserId = adminUserId;
    }

    public void ReopenFlag()
    {
        FlagResolvedAt = null;
        FlagResolvedByUserId = null;
    }

    // Both are one-way: a later status never goes back, and marking again keeps the first time.
    public bool MarkDelivered(DateTimeOffset now)
    {
        if (DeliveredAt is not null)
        {
            return false;
        }

        DeliveredAt = now;
        return true;
    }

    public bool MarkRead(DateTimeOffset now)
    {
        if (ReadAt is not null)
        {
            return false;
        }

        DeliveredAt ??= now;
        ReadAt = now;
        return true;
    }
}

public enum MessageStatus
{
    Sent = 1,
    Delivered = 2,
    Read = 3,
}
