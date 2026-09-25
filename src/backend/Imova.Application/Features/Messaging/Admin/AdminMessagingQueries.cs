using Imova.Contracts.Messaging;
using MediatR;

namespace Imova.Application.Features.Messaging.Admin;

// Admin-only (IsAdmin comes from the caller's JWT role; anyone else gets a 403).

// The admin view has two tabs, Active and Resolved; Resolved = false lists the active ones (newest
// first), true the resolved ones (most recently resolved first).

public record GetMessagingReportsQuery(bool IsAdmin, bool Resolved = false) : IRequest<List<MessagingReportDto>>;

// Messages the content filter flagged.
public record GetFlaggedMessagesQuery(bool IsAdmin, bool Resolved = false) : IRequest<List<FlaggedMessageDto>>;

// A whole conversation, for reviewing a report. Null = no such conversation.
public record GetConversationForAdminQuery(bool IsAdmin, Guid ConversationId) : IRequest<AdminConversationDto?>;

// Resolved = true marks it resolved (by AdminUserId), false reopens it. False = no such report.
public record SetMessagingReportResolvedCommand(bool IsAdmin, Guid AdminUserId, Guid ReportId, bool Resolved) : IRequest<bool>;

// Same for a flagged message. False = no such message, or it isn't flagged.
public record SetFlaggedMessageResolvedCommand(bool IsAdmin, Guid AdminUserId, Guid MessageId, bool Resolved) : IRequest<bool>;

// Banned users can't send messages at all. False = no such user.
public record SetMessagingBanCommand(bool IsAdmin, Guid UserId, bool Banned) : IRequest<bool>;
