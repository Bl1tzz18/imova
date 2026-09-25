using Imova.Application.Common.Identity;

namespace Imova.Application.Common.Interfaces;

public record JwtToken(string Value, DateTimeOffset ExpiresAt);

public interface IJwtTokenGenerator
{
    JwtToken GenerateToken(ApplicationUser user, IList<string> roles);

    // A short-lived token only the realtime (SignalR) hub accepts — handed to browser code, which
    // never sees the long-lived session token (that stays in an httpOnly cookie).
    JwtToken GenerateRealtimeToken(Guid userId);
}
