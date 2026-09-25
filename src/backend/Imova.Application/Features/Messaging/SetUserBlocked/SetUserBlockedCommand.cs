using MediatR;

namespace Imova.Application.Features.Messaging.SetUserBlocked;

// Blocks (or unblocks) the other participant of a conversation: while blocked, no messages go
// either way between the two in any conversation. False = not found / not a participant.
public record SetUserBlockedCommand(Guid UserId, Guid ConversationId, bool Blocked) : IRequest<bool>;
