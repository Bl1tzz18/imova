namespace Imova.Infrastructure.Identity;

// Bound from the "Jwt" configuration section — see appsettings.Development.json for the local dev
// signing key (generated once for this repo) and docker-compose.yml for the same values passed to
// the containerized backend.
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60 * 24 * 7;
}
