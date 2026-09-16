namespace Imova.Application.Common.Interfaces;

// Abstracts Azure Blob Storage (Azurite locally, a real storage account in every deployed
// environment) so Application code never touches the Azure SDK directly. See
// Imova.Infrastructure/Storage/BlobStorageService for the implementation and
// Imova.Infrastructure/Storage/BlobStorageOptions for the per-environment configuration knobs.
public interface IBlobStorageService
{
    // The expiry used when the caller doesn't need a different one — backed by
    // Storage:UploadSasExpiryMinutes so it's configurable per environment without an
    // Application -> Infrastructure dependency on BlobStorageOptions itself.
    TimeSpan DefaultUploadExpiry { get; }

    string GenerateBlobName(Guid propertyId, string fileExtension);

    string GenerateUploadSasUrl(string blobName, TimeSpan expiry);

    string GetPublicUrl(string blobName);

    // Null when the blob hasn't actually been written yet (upload never happened, or failed).
    Task<UploadedBlobInfo?> TryGetUploadedBlobInfoAsync(string blobName, CancellationToken cancellationToken);
}

public record UploadedBlobInfo(long SizeBytes, string? ReportedContentType, byte[] LeadingBytes);
