namespace Imova.Contracts.Listings;

// Url is the large display size (≤1600px), ThumbnailUrl ≤400px, CardUrl ≤800px — all JPEGs. Until
// a photo's sizes exist (just after an upload whose sizes failed, before the backfill), all three
// are the uploaded original. ContentType/FileSizeBytes describe the original.
public record PhotoDto(
    Guid Id,
    Guid ListingId,
    string Url,
    string ThumbnailUrl,
    string CardUrl,
    string ContentType,
    long FileSizeBytes,
    string ModerationStatus,
    int SortOrder,
    bool IsPrimary,
    DateTimeOffset CreatedAt);
