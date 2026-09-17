using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace Imova.Infrastructure.Identity;

// Program.cs configures JwtBearerOptions.MapInboundClaims = false and RoleClaimType = "role", so
// the short claim names written here ("sub", "email", "name", "role") round-trip unchanged instead
// of being silently rewritten to their long-form ClaimTypes.* equivalents on validation.
public class JwtTokenGenerator(JwtOptions options) : IJwtTokenGenerator
{
    public JwtToken GenerateToken(ApplicationUser user, IList<string> roles)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(options.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("name", user.DisplayName ?? user.Email ?? string.Empty),
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
