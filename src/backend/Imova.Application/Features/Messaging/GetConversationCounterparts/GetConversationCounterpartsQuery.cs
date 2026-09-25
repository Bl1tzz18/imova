using MediatR;

namespace Imova.Application.Features.Messaging.GetConversationCounterparts;

// The users the caller has a conversation with — who may see their online status, and whose
// status they may ask for. With ConversationId: only that conversation's other participant
// (empty when the caller isn't in it).
public record GetConversationCounterpartsQuery(Guid UserId, Guid? ConversationId = null) : IRequest<IReadOnlyList<Guid>>;
