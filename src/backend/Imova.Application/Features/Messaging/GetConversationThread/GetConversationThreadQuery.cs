using Imova.Contracts.Messaging;
using MediatR;

namespace Imova.Application.Features.Messaging.GetConversationThread;

// One page of a conversation, oldest first: the latest messages, or those before Before (a
// message id — the oldest one the client already has). Null = not found / not a participant.
public record GetConversationThreadQuery(Guid UserId, Guid ConversationId, Guid? Before = null, int PageSize = 30)
    : IRequest<ConversationThreadDto?>;
