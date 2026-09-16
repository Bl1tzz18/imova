namespace Imova.Contracts.Media;

public record UploadUrlDto(string UploadUrl, string BlobName, DateTimeOffset ExpiresAt);
