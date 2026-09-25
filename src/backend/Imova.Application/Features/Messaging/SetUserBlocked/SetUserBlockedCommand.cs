using MediatR;

namespace Imova.Application.Features.Messaging.SetUserBlocked;

// Blocks (or unblocks) the other participant of a conversation: while blocked they can't send the
// caller messages in any conversation. False = not found / not a participant.
public record SetUserBlockedCommand(Guid UserId, Guid ConversationId, bool Blocked) : IRequest<bool>;
