namespace Imova.Contracts.Listings;

public record PhotoDto(
    Guid Id,
    Guid ListingId,
    string Url,
    string ContentType,
    long FileSizeBytes,
    string ModerationStatus,
    int SortOrder,
    bool IsPrimary,
    DateTimeOffset CreatedAt);
