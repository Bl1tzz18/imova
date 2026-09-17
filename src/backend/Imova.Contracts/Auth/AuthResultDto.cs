namespace Imova.Contracts.Auth;

public record AuthResultDto(string Token, DateTimeOffset ExpiresAt, AuthUserDto User);

public record AuthUserDto(Guid Id, string Email, string? DisplayName, IReadOnlyList<string> Roles);
