using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Imova.Application.Features.Listings.Visitors;

// Who a visitor is, for counting views and phone reveals: their account when signed in, otherwise
// the random id of the web app's anonymous visitor cookie. Only a hash of it is ever stored.
public static partial class ListingVisitors
{
    // What the web app sends: a random id it generated (a UUID today), nothing else.
    [GeneratedRegex("^[A-Za-z0-9-]{8,64}$")]
    private static partial Regex VisitorIdPattern();

    public static bool IsValidVisitorId(string? visitorId) => visitorId is not null && VisitorIdPattern().IsMatch(visitorId);

    // Null when the visitor can't be told apart from others (anonymous without a usable id): such a
    // visit isn't counted at all, rather than counted every time.
    public static string? Hash(Guid? userId, string? visitorId)
    {
        var key = userId is { } id ? $"user:{id}" : IsValidVisitorId(visitorId) ? $"anon:{visitorId}" : null;
        return key is null ? null : Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
    }
}
