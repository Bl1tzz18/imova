using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Imova.Application.Features.Auth.Sessions;

namespace Imova.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Authenticated request is missing the user id claim."));

    // The login session the caller's token belongs to (see AuthSessions) — null for a token without one.
    public static Guid? GetSessionId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(AuthSessions.SessionIdClaim), out var id) ? id : null;
}
