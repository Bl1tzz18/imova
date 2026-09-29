namespace Imova.Contracts.Admins;

// GrantedAt/GrantedBy: from the audit trail — null for an admin made before it existed (by SQL).
public record AdminUserDto(Guid Id, string Email, string? DisplayName, DateTimeOffset? GrantedAt, string? GrantedBy);
