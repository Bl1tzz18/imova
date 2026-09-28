using Imova.Application.Features.Auth.Sessions;
using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Auth.RefreshSession;

public class RefreshSessionHandler(AuthSessions authSessions) : IRequestHandler<RefreshSessionCommand, SessionTokenDto>
{
    public Task<SessionTokenDto> Handle(RefreshSessionCommand request, CancellationToken cancellationToken) =>
        authSessions.RefreshAsync(request.RefreshToken, cancellationToken);
}
