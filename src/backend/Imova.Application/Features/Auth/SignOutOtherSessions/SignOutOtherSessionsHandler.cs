using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Auth.Sessions;
using Imova.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth.SignOutOtherSessions;

public class SignOutOtherSessionsHandler(UserManager<ApplicationUser> userManager, AuthSessions authSessions)
    : IRequestHandler<SignOutOtherSessionsCommand, SessionTokenDto>
{
    public async Task<SessionTokenDto> Handle(SignOutOtherSessionsCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new AuthenticationFailedException("User not found.", ErrorCodes.UserNotFound);

        await userManager.UpdateSecurityStampAsync(user);

        return await authSessions.ContinueAsync(user, request.SessionId, cancellationToken);
    }
}
