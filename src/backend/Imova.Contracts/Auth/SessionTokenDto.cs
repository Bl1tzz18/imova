namespace Imova.Contracts.Auth;

// A new login token for the session that asked for a change that signed every session out
// (a password change, "sign out other sessions") — so that one session stays signed in.
public record SessionTokenDto(string Token, DateTimeOffset ExpiresAt);

public record ChangePasswordResultDto(UserProfileDto Profile, string Token, DateTimeOffset ExpiresAt);
