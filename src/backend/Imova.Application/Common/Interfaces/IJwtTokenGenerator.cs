using Imova.Application.Common.Identity;

namespace Imova.Application.Common.Interfaces;

public record JwtToken(string Value, DateTimeOffset ExpiresAt);

public interface IJwtTokenGenerator
{
    JwtToken GenerateToken(ApplicationUser user, IList<string> roles);
}
