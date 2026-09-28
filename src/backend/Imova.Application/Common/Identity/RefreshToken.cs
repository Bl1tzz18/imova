namespace Imova.Application.Common.Identity;

// One refresh token of a login session (see AuthSessions). A session is a chain of these: each use
// replaces the token with a new one (rotation), so a stolen token that gets used after its owner
// already used it is recognised — and ends the whole session. Only a SHA-256 hash of the token is
// stored; the token itself lives in the user's httpOnly cookie.
public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    // The login session this token belongs to — the same for every token in the chain, and the
    // "sid" claim of the login tokens issued with it.
    public Guid SessionId { get; set; }

    public required string TokenHash { get; set; }

    // "Ține-mă minte": stays signed in for AuthSessionOptions.RememberedDays of inactivity (a lasting
    // cookie) instead of AuthSessionOptions.SessionHours (a browser-session cookie).
    public bool Persistent { get; set; }

    // SessionStamp.For(security stamp) when issued: a password change/reset or "sign out other
    // devices" changes the stamp and so invalidates every token issued before it.
    public required string StampFingerprint { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Idle limit: this token must be used by then (each use starts a new period).
    public DateTimeOffset ExpiresAt { get; set; }

    // Absolute limit for the whole session, however active (AuthSessionOptions.MaxDays).
    public DateTimeOffset SessionExpiresAt { get; set; }

    // Set when this token was exchanged for the next one.
    public DateTimeOffset? ReplacedAt { get; set; }

    // Set when the session was ended (sign-out, or a replayed token).
    public DateTimeOffset? RevokedAt { get; set; }
}
