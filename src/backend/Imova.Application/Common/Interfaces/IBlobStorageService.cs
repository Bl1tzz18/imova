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

    // Prefixed under "profile-pictures/" so these never collide with the flat "{propertyId}/..."
    // scheme GenerateBlobName uses — both share the same container.
    string GenerateProfilePictureBlobName(Guid userId, string fileExtension);

    string GenerateUploadSasUrl(string blobName, TimeSpan expiry);

    string GetPublicUrl(string blobName);

    // Inverse of GetPublicUrl — recovers the blob name from a URL this service previously
    // returned, so a caller that only stored the URL (e.g. ApplicationUser.ProfilePictureUrl)
    // can still delete that blob later. Null if the URL isn't one of ours.
    string? TryGetBlobNameFromUrl(string url);

    // Null when the blob hasn't actually been written yet (upload never happened, or failed).
    Task<UploadedBlobInfo?> TryGetUploadedBlobInfoAsync(string blobName, CancellationToken cancellationToken);

    // Direct server-side write — for content the server itself produced or fetched (e.g. a
    // downloaded Google profile picture, or a profile picture received via a server-validated
    // multipart upload), as opposed to GenerateUploadSasUrl's browser-direct-to-storage flow.
    Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken);

    Task DeleteAsync(string blobName, CancellationToken cancellationToken);
}

public record UploadedBlobInfo(long SizeBytes, string? ReportedContentType, byte[] LeadingBytes);
