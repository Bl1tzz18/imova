using MediatR;

namespace Imova.Application.Features.Messaging.MarkConversationRead;

// The caller has seen the thread: every message sent to them in it becomes Read. False = not
// found / not a participant.
public record MarkConversationReadCommand(Guid UserId, Guid ConversationId) : IRequest<bool>;
