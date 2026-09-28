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

    // The login token's lifetime. Short on purpose: the web app renews it from the session's
    // refresh token (see AuthSessions), and a leaked one is only useful for minutes.
    public int ExpiryMinutes { get; set; } = 15;

    // Realtime (SignalR) tokens: their own audience, so the hub accepts nothing else and the REST
    // API doesn't accept them (see GenerateRealtimeToken).
    public string RealtimeAudience => $"{Audience}.Realtime";

    public int RealtimeExpiryMinutes { get; set; } = 15;
}
