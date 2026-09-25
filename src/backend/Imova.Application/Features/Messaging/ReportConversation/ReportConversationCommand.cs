using Imova.Domain.Messaging;
using MediatR;

namespace Imova.Application.Features.Messaging.ReportConversation;

// Details is required for ReportReason.Other. False = not found / not a participant.
public record ReportConversationCommand(Guid UserId, Guid ConversationId, ReportReason Reason, string? Details) : IRequest<bool>;
