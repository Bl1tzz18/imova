using Imova.Contracts.Messaging;
using MediatR;

namespace Imova.Application.Features.Messaging.GetRealtimeToken;

public record GetRealtimeTokenQuery(Guid UserId) : IRequest<RealtimeTokenDto>;
