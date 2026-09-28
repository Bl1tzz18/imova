namespace Imova.Contracts.Auth;

// Sign-in / registration result: the session's tokens (see SessionTokenDto) and who signed in.
public record AuthResultDto(
    string Token,
    DateTimeOffset ExpiresAt,
    AuthUserDto User,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    bool Persistent);

public record AuthUserDto(
    Guid Id,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles,
    bool RequiresPhoneNumber,
    string? ProfilePictureUrl,
    bool EmailConfirmed);
