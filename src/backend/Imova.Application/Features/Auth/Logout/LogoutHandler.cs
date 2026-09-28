using Imova.Application.Features.Auth.Sessions;
using MediatR;

namespace Imova.Application.Features.Auth.Logout;

public class LogoutHandler(AuthSessions authSessions) : IRequestHandler<LogoutCommand>
{
    public Task Handle(LogoutCommand request, CancellationToken cancellationToken) =>
        authSessions.EndAsync(request.RefreshToken, cancellationToken);
}
