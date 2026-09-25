using Imova.Domain.Common;

namespace Imova.Domain.Messaging;

// A participant flagging a conversation for admins. Stays open until an admin resolves it.
public sealed class ConversationReport : Entity
{
    public const int MaxDetailsLength = 1000;

    // For EF Core materialization only.
    private ConversationReport()
        : base(Guid.Empty)
    {
    }

    private ConversationReport(Guid id, Guid conversationId, Guid reporterUserId, ReportReason reason, string? details, DateTimeOffset now)
        : base(id)
    {
        ConversationId = conversationId;
        ReporterUserId = reporterUserId;
        Reason = reason;
        Details = details;
        CreatedAt = now;
    }

    public Guid ConversationId { get; private set; }

    public Guid ReporterUserId { get; private set; }

    public ReportReason Reason { get; private set; }

    public string? Details { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public Guid? ResolvedByUserId { get; private set; }

    public static ConversationReport Create(
        Conversation conversation, Guid reporterUserId, ReportReason reason, string? details, DateTimeOffset now)
    {
        if (!conversation.IsParticipant(reporterUserId))
        {
            throw new InvalidOperationException("Only a participant can report a conversation.");
        }

        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown report reason.");
        }

        details = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
        if (reason == ReportReason.Other && details is null)
        {
            throw new ArgumentException("Describe the problem when the reason is Other.", nameof(details));
        }

        return new ConversationReport(Guid.NewGuid(), conversation.Id, reporterUserId, reason, details, now);
    }

    public void Resolve(Guid adminUserId, DateTimeOffset now)
    {
        ResolvedAt ??= now;
        ResolvedByUserId ??= adminUserId;
    }
}

public enum ReportReason
{
    Spam = 1,
    Fraud = 2,
    Abuse = 3,
    Other = 4,
}
