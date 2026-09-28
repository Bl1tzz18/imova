using System.Buffers.Text;
using System.Text;

namespace Imova.Application.Features.Auth;

// Identity's password-reset and email-confirmation tokens contain '+', '/' and '=', which don't
// survive a URL (or an email client) intact, so the links we send carry them Base64Url-encoded.
public static class AccountTokens
{
    public static string Encode(string token) => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(token));

    // Null for anything that isn't valid Base64Url — callers treat that like an expired link.
    public static string? Decode(string encoded)
    {
        try
        {
            return Encoding.UTF8.GetString(Base64Url.DecodeFromChars(encoded));
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
