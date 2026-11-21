using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Features.Listings;
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
        var after = DateTimeOffset.UtcNow;

        // Bounded by the clock before and after the call, however long the call took (the first
        // in-memory database of a test run can take seconds to build).
        Assert.InRange(result.ExpiresAt, before.Add(blobStorage.DefaultUploadExpiry), after.Add(blobStorage.DefaultUploadExpiry));
    }

    [Fact]
    public async Task Handle_WhenTheListingHasTheMostPhotosAllowed_GivesNoUploadUrl()
    {
        var blobStorage = new FakeBlobStorageService();
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new RequestUploadUrlHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();
        await ConfirmMediaUploadHandlerTests.AddPhotosAsync(dbContext, listingId, ListingPhotoRules.MaxPhotos);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new RequestUploadUrlCommand(listingId, ".jpg", ConfirmMediaUploadHandlerTests.UploaderId, false), CancellationToken.None));

        var failure = Assert.Single(ex.Errors);
        Assert.Equal(ErrorCodes.UploadTooManyPhotos, failure.ErrorCode);
        Assert.Equal(nameof(RequestUploadUrlCommand.ListingId), failure.PropertyName);
    }
}
