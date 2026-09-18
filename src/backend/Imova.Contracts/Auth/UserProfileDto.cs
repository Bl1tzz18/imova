namespace Imova.Contracts.Auth;

public record UserProfileDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string? PhoneNumber,
    IReadOnlyList<string> Roles,
    bool HasPassword);
