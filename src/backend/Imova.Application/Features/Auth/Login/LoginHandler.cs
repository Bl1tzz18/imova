using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth.Login;

public class LoginHandler(UserManager<ApplicationUser> userManager, IJwtTokenGenerator jwtTokenGenerator)
    : IRequestHandler<LoginCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new AuthenticationFailedException("Invalid email or password.");
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var token = jwtTokenGenerator.GenerateToken(user, roles);

        return new AuthResultDto(
            token.Value,
            token.ExpiresAt,
            new AuthUserDto(user.Id, user.Email!, user.DisplayName, roles, string.IsNullOrWhiteSpace(user.PhoneNumber)));
    }
}
