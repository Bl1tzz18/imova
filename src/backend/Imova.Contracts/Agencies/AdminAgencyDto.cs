namespace Imova.Contracts.Agencies;

// An agency in the admins' list (GET /api/v1/admin/agencies). Status: Active | Deactivated.
// OwnerName/OwnerEmail: its longest-standing Owner (null if none can be found).
public record AdminAgencyDto(
    Guid Id,
    string Slug,
    string Name,
    string? LogoThumbnailUrl,
    bool IsVerified,
    DateTimeOffset? VerifiedAt,
    string Status,
    string? RaionName,
    int MemberCount,
    int ActiveListingCount,
    DateTimeOffset CreatedAt,
    string? OwnerName,
    string? OwnerEmail);
