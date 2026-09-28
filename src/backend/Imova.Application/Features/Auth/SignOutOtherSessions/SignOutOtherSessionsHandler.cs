using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth.SignOutOtherSessions;

public class SignOutOtherSessionsHandler(UserManager<ApplicationUser> userManager, IJwtTokenGenerator jwtTokenGenerator)
    : IRequestHandler<SignOutOtherSessionsCommand, SessionTokenDto>
{
    public async Task<SessionTokenDto> Handle(SignOutOtherSessionsCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new AuthenticationFailedException("User not found.", ErrorCodes.UserNotFound);

        await userManager.UpdateSecurityStampAsync(user);

        var token = jwtTokenGenerator.GenerateToken(user, await userManager.GetRolesAsync(user));
        return new SessionTokenDto(token.Value, token.ExpiresAt);
    }
}
