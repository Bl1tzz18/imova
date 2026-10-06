namespace Imova.Contracts.Agencies;

// An agency's profile. Phone is only for its members and admins — the public gets PhonePrefix and
// PhoneHiddenDigits (its shape) and asks for the number itself, one request at a time, like a
// listing's phone. LogoUrl is the 512 px square, LogoThumbnailUrl the 128 px one (both null without
// a logo). Status: Active | Deactivated. MyRole: the caller's role (Owner | Admin | Agent), null
// for a non-member.
public record AgencyDto(
    Guid Id,
    string Slug,
    string Name,
    string? LogoUrl,
    string? LogoThumbnailUrl,
    string? Bio,
    string? Phone,
    string? PhonePrefix,
    int? PhoneHiddenDigits,
    string Email,
    string? Website,
    string? Address,
    Guid? RaionId,
    string? RaionName,
    bool IsVerified,
    DateTimeOffset? VerifiedAt,
    string Status,
    DateTimeOffset CreatedAt,
    int MemberCount,
    int ActiveListingCount,
    string? MyRole);

// An agency in the public directory (GET /api/v1/agencies). LogoThumbnailUrl: the 128 px square.
public record AgencyCardDto(
    Guid Id,
    string Slug,
    string Name,
    string? LogoThumbnailUrl,
    bool IsVerified,
    string? RaionName,
    int ActiveListingCount);

// GET /api/v1/agencies/by-slug/{slug} for a slug the agency used before a rename (with a 301).
public record AgencySlugRedirectDto(string Slug);

// An agency's full phone number, asked for one at a time (POST /api/v1/agencies/{id}/contact/phone).
public record AgencyPhoneDto(string Phone);
