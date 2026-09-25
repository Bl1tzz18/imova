using MediatR;

namespace Imova.Application.Features.Messaging.SetConversationArchived;

// Archive hides the conversation from the caller's inbox only; the other participant is
// unaffected, and a new message brings it back. False = not found / not a participant.
public record SetConversationArchivedCommand(Guid UserId, Guid ConversationId, bool Archived) : IRequest<bool>;
