using Imova.Application.Features.Media.RequestUploadUrl;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Media;

public class RequestUploadUrlHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAUrlAndBlobNameFromTheBlobStorageService()
    {
        var blobStorage = new FakeBlobStorageService();
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new RequestUploadUrlHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();

        var result = await handler.Handle(new RequestUploadUrlCommand(listingId, ".jpg", Guid.NewGuid(), false), CancellationToken.None);

        Assert.Equal(blobStorage.GenerateBlobName(listingId, ".jpg"), result.BlobName);
        Assert.Equal(blobStorage.GenerateUploadSasUrl(result.BlobName, blobStorage.DefaultUploadExpiry), result.UploadUrl);
    }

    [Fact]
    public async Task Handle_SetsExpiresAtBasedOnDefaultUploadExpiry()
    {
        var blobStorage = new FakeBlobStorageService();
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new RequestUploadUrlHandler(dbContext, blobStorage);
        var before = DateTimeOffset.UtcNow;

        var result = await handler.Handle(new RequestUploadUrlCommand(Guid.NewGuid(), ".jpg", Guid.NewGuid(), false), CancellationToken.None);

        var expectedExpiry = before.Add(blobStorage.DefaultUploadExpiry);
        Assert.True(Math.Abs((result.ExpiresAt - expectedExpiry).TotalSeconds) < 5);
    }
}
