namespace Imova.Contracts.Media;

public record PropertyMediaDto(
    Guid Id,
    Guid PropertyId,
    string Url,
    string ContentType,
    long FileSizeBytes,
    string ModerationStatus,
    int SortOrder,
    DateTimeOffset CreatedAt);
