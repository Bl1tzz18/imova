namespace Imova.Contracts.Auth;

// A signed-in session's tokens: the short-lived login token (sent as "Bearer") and the refresh
// token that renews it (POST /api/v1/auth/refresh). Persistent = "remember me": keep the refresh
// token in a lasting cookie until RefreshTokenExpiresAt, instead of one that ends with the browser.
public record SessionTokenDto(
    string Token,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    bool Persistent);

// A password change signs out every other session; Session is the continuation of this one.
public record ChangePasswordResultDto(UserProfileDto Profile, SessionTokenDto Session);
