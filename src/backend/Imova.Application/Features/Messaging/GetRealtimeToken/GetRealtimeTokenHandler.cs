using Imova.Application.Common.Interfaces;
using Imova.Contracts.Messaging;
using MediatR;

namespace Imova.Application.Features.Messaging.GetRealtimeToken;

public class GetRealtimeTokenHandler(IJwtTokenGenerator tokenGenerator) : IRequestHandler<GetRealtimeTokenQuery, RealtimeTokenDto>
{
    public Task<RealtimeTokenDto> Handle(GetRealtimeTokenQuery request, CancellationToken cancellationToken)
    {
        var token = tokenGenerator.GenerateRealtimeToken(request.UserId);
        return Task.FromResult(new RealtimeTokenDto(token.Value, token.ExpiresAt));
    }
}
