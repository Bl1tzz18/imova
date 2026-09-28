using Imova.Application.Common;
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
    public const string InvalidCredentials = "Invalid email or password.";

    public const string LockedOut =
        "Too many failed sign-in attempts. Try again in a few minutes, or reset your password.";

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new AuthenticationFailedException(InvalidCredentials, ErrorCodes.InvalidCredentials);

        // Identity's lockout (IdentityOptions.Lockout, set in Program.cs): after too many wrong
        // passwords the account refuses sign-in for a while, even with the right one — the per-IP
        // rate limit alone doesn't stop a guesser spread over many IPs. A password reset lifts it.
        if (await userManager.IsLockedOutAsync(user))
        {
            throw new TooManyRequestsException(LockedOut, ErrorCodes.LockedOut);
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            if (await userManager.IsLockedOutAsync(user))
            {
                throw new TooManyRequestsException(LockedOut, ErrorCodes.LockedOut);
            }

            throw new AuthenticationFailedException(InvalidCredentials, ErrorCodes.InvalidCredentials);
        }

        if (await userManager.GetAccessFailedCountAsync(user) > 0)
        {
            await userManager.ResetAccessFailedCountAsync(user);
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var token = jwtTokenGenerator.GenerateToken(user, roles);

        return new AuthResultDto(
            token.Value,
            token.ExpiresAt,
            new AuthUserDto(
                user.Id,
                user.Email!,
                user.DisplayName,
                roles,
                string.IsNullOrWhiteSpace(user.PhoneNumber),
                user.ProfilePictureUrl,
                user.EmailConfirmed));
    }
}
