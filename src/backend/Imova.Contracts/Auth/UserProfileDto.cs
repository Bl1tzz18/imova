namespace Imova.Contracts.Auth;

public record UserProfileDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string? PhoneNumber,
    string? ProfilePictureUrl,
    IReadOnlyList<string> Roles,
    bool HasPassword);
