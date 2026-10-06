using System.Security.Cryptography;
using System.Text;

namespace Imova.Application.Features.Agencies.Invitations;

// An invitation link carries a random 32-byte token (Base64Url); the database only keeps its SHA-256,
// so a leaked database can't be turned into working links.
public static class AgencyInvitationTokens
{
    public static (string Token, string Hash) New()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (token, Hash(token));
    }

    public static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
