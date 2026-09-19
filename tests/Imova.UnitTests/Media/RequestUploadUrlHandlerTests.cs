using Imova.Application.Features.Media.RequestUploadUrl;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Media;

public class RequestUploadUrlHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAUrlAndBlobNameFromTheBlobStorageService()
    {
        var blobStorage = new FakeBlobStorageService();
        var handler = new RequestUploadUrlHandler(blobStorage);
        var propertyId = Guid.NewGuid();

        var result = await handler.Handle(new RequestUploadUrlCommand(propertyId, ".jpg"), CancellationToken.None);

        Assert.Equal(blobStorage.GenerateBlobName(propertyId, ".jpg"), result.BlobName);
        Assert.Equal(blobStorage.GenerateUploadSasUrl(result.BlobName, blobStorage.DefaultUploadExpiry), result.UploadUrl);
    }

    [Fact]
    public async Task Handle_SetsExpiresAtBasedOnDefaultUploadExpiry()
    {
        var blobStorage = new FakeBlobStorageService();
        var handler = new RequestUploadUrlHandler(blobStorage);
        var before = DateTimeOffset.UtcNow;

        var result = await handler.Handle(new RequestUploadUrlCommand(Guid.NewGuid(), ".jpg"), CancellationToken.None);

        var expectedExpiry = before.Add(blobStorage.DefaultUploadExpiry);
        Assert.True(Math.Abs((result.ExpiresAt - expectedExpiry).TotalSeconds) < 5);
    }
}
