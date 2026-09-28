namespace Imova.Application.Features.Auth.Sessions;

// Bound from "Sessions". How long a sign-in lasts; the login token itself (Jwt:ExpiryMinutes,
// 15 minutes) is renewed from the refresh token long before any of these run out.
public class AuthSessionOptions
{
    public const string SectionName = "Sessions";

    // "Ține-mă minte" ticked: signed in until this many days pass without a visit.
    public int RememberedDays { get; set; } = 30;

    // Not ticked: the cookie ends with the browser, and the server gives up after this long unused.
    public int SessionHours { get; set; } = 24;

    // However active, a session asks for the password again after this long.
    public int MaxDays { get; set; } = 90;

    // Two requests racing to refresh with the same token (e.g. several tabs, or prefetches when the
    // login token has just run out) are normal, not theft: a token replaced less than this long
    // ago may still be exchanged.
    public int ReuseGraceSeconds { get; set; } = 60;
}
