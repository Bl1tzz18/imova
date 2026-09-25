namespace Imova.Contracts.Messaging;

public record MessageAttachmentDto(Guid Id, string Url, string ContentType);

// Status: Sent | Delivered | Read (for the recipient — there is exactly one).
public record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string Body,
    IReadOnlyList<MessageAttachmentDto> Attachments,
    DateTimeOffset CreatedAt,
    string Status);

public record ConversationParticipantDto(Guid UserId, string DisplayName, string? AvatarUrl);

// Title is null when the listing has since been deleted (the conversation stays).
public record ConversationListingDto(Guid Id, string? Title, string? PhotoUrl);

// One inbox row, from the caller's point of view (OtherParticipant, UnreadCount, IsArchived).
// IsInitiator: the caller started it (they're the visitor, not the listing's publisher).
public record ConversationSummaryDto(
    Guid Id,
    ConversationListingDto Listing,
    ConversationParticipantDto OtherParticipant,
    MessageDto? LastMessage,
    int UnreadCount,
    DateTimeOffset LastMessageAt,
    bool IsArchived,
    bool IsInitiator);

// A page of a thread, oldest first. HasMore: older messages exist (load them with ?before=<first id>).
public record ConversationThreadDto(
    ConversationSummaryDto Conversation,
    IReadOnlyList<MessageDto> Messages,
    bool HasMore,
    bool BlockedByMe,
    bool BlockedByOther);

public record StartConversationResultDto(Guid ConversationId, MessageDto Message, bool Reused);

public record MessageStatusChangedDto(Guid ConversationId, IReadOnlyList<Guid> MessageIds, string Status);

public record UnreadCountDto(int Count);

public record AttachmentUploadUrlDto(string UploadUrl, string BlobName, DateTimeOffset ExpiresAt);

public record RealtimeTokenDto(string Token, DateTimeOffset ExpiresAt);

// --- Admin ---

public record MessagingUserDto(Guid Id, string? DisplayName, string? Email, bool IsBannedFromMessaging);

// Reason: Spam | Fraud | Abuse | Other. ReportedUser is the other participant. ResolvedBy is the
// admin who resolved it (null while active).
public record MessagingReportDto(
    Guid Id,
    Guid ConversationId,
    string Reason,
    string? Details,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    MessagingUserDto Reporter,
    MessagingUserDto ReportedUser,
    ConversationListingDto Listing,
    MessagingUserDto? ResolvedBy);

// ResolvedAt/ResolvedBy: an admin reviewed the flag (null while active).
public record FlaggedMessageDto(
    MessageDto Message,
    string FlagReason,
    MessagingUserDto Sender,
    DateTimeOffset? ResolvedAt,
    MessagingUserDto? ResolvedBy);

public record AdminConversationDto(
    Guid Id,
    ConversationListingDto Listing,
    MessagingUserDto Initiator,
    MessagingUserDto Publisher,
    IReadOnlyList<MessageDto> Messages);

// --- Realtime (SignalR) event payloads ---

public record TypingDto(Guid ConversationId, Guid UserId);

public record PresenceDto(Guid UserId, bool Online);
