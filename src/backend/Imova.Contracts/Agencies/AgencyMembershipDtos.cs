namespace Imova.Contracts.Agencies;

// A member as the agency's other members see them. Role: Owner | Admin | Agent. ListingCount: the
// listings they authored under the agency (what would be handed on if they left).
public record AgencyMemberDto(
    Guid UserId,
    string Name,
    string Email,
    string? PictureUrl,
    string Role,
    DateTimeOffset JoinedAt,
    int ListingCount);

// An invitation as the agency's Owners/Admins see it. Status: Pending | Expired (the open ones).
// EmailFailedAt: set when its latest email couldn't be sent — resend it.
public record AgencyInvitationDto(
    Guid Id,
    string Email,
    string Role,
    string Status,
    DateTimeOffset LastSentAt,
    DateTimeOffset ExpiresAt,
    string? InvitedByName,
    DateTimeOffset? EmailFailedAt);

// An invitation as its recipient sees it (the invitation page, "my invitations").
// Status: Pending | Accepted | Declined | Revoked | Expired. AgencyIsActive: false while the agency is
// deactivated (it can still be joined; the page says it's inactive for now).
public record InvitationDto(
    Guid Id,
    Guid AgencyId,
    string AgencyName,
    string AgencySlug,
    string? AgencyLogoUrl,
    bool AgencyIsVerified,
    bool AgencyIsActive,
    string Role,
    string? InvitedByName,
    string Email,
    string Status,
    DateTimeOffset ExpiresAt);

// An agency the caller belongs to, and their role in it.
public record MyAgencyDto(
    Guid Id,
    string Name,
    string Slug,
    string? LogoThumbnailUrl,
    bool IsVerified,
    string Status,
    string Role);
