namespace Imova.Infrastructure.Identity;

// Bound from the "GoogleAuth" configuration section. ClientId is used (by GoogleTokenValidator, to
// check the ID token's audience, and exposed to the frontend via GET /api/v1/auth/config so it can
// initialize Google's Sign-In button without needing its own copy of the value). ClientSecret isn't
// needed for verifying an ID token — Google.Apis.Auth only checks the token's signature and
// audience — but is kept here since the sign-in flow could later grow to need a server-side OAuth
// code exchange, which does need it.
public class GoogleAuthOptions
{
    public const string SectionName = "GoogleAuth";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;
}
