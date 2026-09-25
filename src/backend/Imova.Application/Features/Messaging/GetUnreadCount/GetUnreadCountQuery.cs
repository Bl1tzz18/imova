using Imova.Contracts.Messaging;
using MediatR;

namespace Imova.Application.Features.Messaging.GetUnreadCount;

// Messages sent to the caller they haven't read, across all their conversations (header badge).
public record GetUnreadCountQuery(Guid UserId) : IRequest<UnreadCountDto>;
