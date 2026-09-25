using Imova.Contracts.Messaging;
using MediatR;

namespace Imova.Application.Features.Messaging.Admin;

// Admin-only (IsAdmin comes from the caller's JWT role; anyone else gets a 403).

// Reports, newest first; resolved ones only when asked for.
public record GetMessagingReportsQuery(bool IsAdmin, bool IncludeResolved = false) : IRequest<List<MessagingReportDto>>;

// Messages the content filter flagged, newest first.
public record GetFlaggedMessagesQuery(bool IsAdmin) : IRequest<List<FlaggedMessageDto>>;

// A whole conversation, for reviewing a report. Null = no such conversation.
public record GetConversationForAdminQuery(bool IsAdmin, Guid ConversationId) : IRequest<AdminConversationDto?>;

// False = no such report.
public record ResolveMessagingReportCommand(bool IsAdmin, Guid AdminUserId, Guid ReportId) : IRequest<bool>;

// Banned users can't send messages at all. False = no such user.
public record SetMessagingBanCommand(bool IsAdmin, Guid UserId, bool Banned) : IRequest<bool>;
