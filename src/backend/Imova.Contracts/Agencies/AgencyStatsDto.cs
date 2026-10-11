namespace Imova.Contracts.Agencies;

// GET /api/v1/agencies/{id}/stats — an agency's results over a period of Moldovan days (From..To,
// both included), led by contacts: someone asked for a phone number (a listing's or the agency's own)
// or started a conversation. Previous: the same totals for the period of the same length just before,
// for the comparison. For an Agent: only their own listings (and no agency-page reveals).
public record AgencyStatsDto(
    DateOnly From,
    DateOnly To,
    AgencyStatsTotalsDto Totals,
    AgencyStatsTotalsDto Previous,
    IReadOnlyList<AgencyStatsDayDto> Daily,
    IReadOnlyList<AgencyStatsListingDto> TopListings,
    IReadOnlyList<AgencyStatsAttentionDto> NeedsAttention);

// Contacts = ListingPhoneReveals + AgencyPhoneReveals + Messages.
public record AgencyStatsTotalsDto(
    int Contacts,
    int Views,
    int ListingPhoneReveals,
    int AgencyPhoneReveals,
    int Messages,
    int Favorites);

public record AgencyStatsDayDto(DateOnly Day, int Views, int Contacts);

// ThumbnailUrl: the listing's cover photo (400 px), null without photos.
public record AgencyStatsListingDto(
    Guid ListingId,
    long Number,
    string Title,
    string Status,
    string? ThumbnailUrl,
    int Views,
    int PhoneReveals,
    int Messages,
    int Favorites);

// Reason: NoContacts (views but nobody got in touch) | ExpiringSoon | FewPhotos. Views: in the period.
public record AgencyStatsAttentionDto(Guid ListingId, long Number, string Title, string Reason, int Views, DateTimeOffset? ExpiresAt);
