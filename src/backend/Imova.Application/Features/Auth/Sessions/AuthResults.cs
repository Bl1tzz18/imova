using Imova.Application.Common.Identity;
using Imova.Contracts.Auth;

namespace Imova.Application.Features.Auth.Sessions;

public static class AuthResults
{
    public static AuthResultDto For(ApplicationUser user, IReadOnlyList<string> roles, SessionTokenDto session) =>
        new(
            session.Token,
            session.ExpiresAt,
            new AuthUserDto(
                user.Id,
                user.Email!,
                user.DisplayName,
                roles,
                string.IsNullOrWhiteSpace(user.PhoneNumber),
                user.ProfilePictureUrl,
                user.EmailConfirmed),
            session.RefreshToken,
            session.RefreshTokenExpiresAt,
            session.Persistent);
}
