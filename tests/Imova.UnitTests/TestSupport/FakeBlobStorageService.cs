using Imova.Application.Common.Interfaces;

namespace Imova.UnitTests.TestSupport;

internal sealed class FakeBlobStorageService : IBlobStorageService
{
    public bool UploadCalled { get; private set; }

    public string? UploadedBlobName { get; private set; }

    public string? UploadedContentType { get; private set; }

    public TimeSpan DefaultUploadExpiry => TimeSpan.FromMinutes(15);

    public string GenerateBlobName(Guid propertyId, string fileExtension) => $"{propertyId}.{fileExtension}";

    public string GenerateProfilePictureBlobName(Guid userId, string fileExtension) =>
        $"profile-pictures/{userId}.{fileExtension}";

    public string GenerateUploadSasUrl(string blobName, TimeSpan expiry) => $"https://blob.test/{blobName}?sas";

    public string GetPublicUrl(string blobName) => $"https://blob.test/{blobName}";

    public string? TryGetBlobNameFromUrl(string url) =>
        url.StartsWith("https://blob.test/", StringComparison.Ordinal) ? url["https://blob.test/".Length..] : null;

    public Task<UploadedBlobInfo?> TryGetUploadedBlobInfoAsync(string blobName, CancellationToken cancellationToken) =>
        Task.FromResult<UploadedBlobInfo?>(null);

    public Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken)
    {
        UploadCalled = true;
        UploadedBlobName = blobName;
        UploadedContentType = contentType;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string blobName, CancellationToken cancellationToken) => Task.CompletedTask;
}
