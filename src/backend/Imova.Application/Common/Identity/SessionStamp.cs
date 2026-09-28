using System.Security.Cryptography;
using System.Text;
using Imova.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Common.Identity;

// Ties a login token to the account's Identity security stamp, so a token stops working as soon as
// the stamp changes — Identity changes it on every password change, password reset and first
// password (and SignOutOtherSessions changes it on purpose). JwtTokenGenerator writes the "sst"
// claim; the API checks it on every authenticated request (Program.cs, JwtBearer OnTokenValidated).
//
// The claim is a fingerprint of the stamp, not the stamp itself: a JWT's payload is readable by
// whoever holds it, and the raw stamp has no business leaving the database.
public static class SessionStamp
{
    public const string ClaimType = "sst";

    public static string For(string? securityStamp) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(securityStamp ?? string.Empty)))[..32];

    // False for a token without the claim (issued before this check existed), for an account that
    // no longer exists, and for a stamp that has changed since the token was issued.
    public static async Task<bool> IsCurrentAsync(
        IApplicationDbContext dbContext, Guid userId, string? claim, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(claim))
        {
            return false;
        }

        var user = await dbContext.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.SecurityStamp })
            .FirstOrDefaultAsync(cancellationToken);

        return user is not null
            && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(For(user.SecurityStamp)), Encoding.ASCII.GetBytes(claim));
    }
}
