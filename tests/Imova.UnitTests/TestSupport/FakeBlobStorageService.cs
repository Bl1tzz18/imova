using Imova.Application.Common.Interfaces;

namespace Imova.UnitTests.TestSupport;

internal sealed class FakeBlobStorageService : IBlobStorageService
{
    public bool UploadCalled { get; private set; }

    public string? UploadedBlobName { get; private set; }

    public string? UploadedContentType { get; private set; }

    public List<string> DeletedBlobNames { get; } = [];

    public UploadedBlobInfo? BlobInfoToReturn { get; set; }

    // Per-blob answers for TryGetUploadedBlobInfoAsync; falls back to BlobInfoToReturn.
    public Dictionary<string, UploadedBlobInfo> BlobInfoByName { get; } = [];

    public TimeSpan DefaultUploadExpiry => TimeSpan.FromMinutes(15);

    public string GenerateBlobName(Guid listingId, string fileExtension) => $"{listingId}.{fileExtension}";

    public string GenerateProfilePictureBlobName(Guid userId, string fileExtension) =>
        $"profile-pictures/{userId}.{fileExtension}";

    public string GenerateMessageAttachmentBlobName(Guid senderUserId, string fileExtension) =>
        $"messages/{senderUserId}/{Guid.NewGuid()}{fileExtension}";

    public string GenerateUploadSasUrl(string blobName, TimeSpan expiry) => $"https://blob.test/{blobName}?sas";

    public string GetPublicUrl(string blobName) => $"https://blob.test/{blobName}";

    public string? TryGetBlobNameFromUrl(string url) =>
        url.StartsWith("https://blob.test/", StringComparison.Ordinal) ? url["https://blob.test/".Length..] : null;

    public Task<UploadedBlobInfo?> TryGetUploadedBlobInfoAsync(string blobName, CancellationToken cancellationToken) =>
        Task.FromResult(BlobInfoByName.GetValueOrDefault(blobName) ?? BlobInfoToReturn);

    public Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken)
    {
        UploadCalled = true;
        UploadedBlobName = blobName;
        UploadedContentType = contentType;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string blobName, CancellationToken cancellationToken)
    {
        DeletedBlobNames.Add(blobName);
        return Task.CompletedTask;
    }
}
