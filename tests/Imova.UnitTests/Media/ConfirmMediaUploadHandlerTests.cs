using FluentValidation;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Media.ConfirmMediaUpload;
using Imova.Domain.Listings;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Media;

public class ConfirmMediaUploadHandlerTests
{
    // Minimal valid PNG signature — see ImageSignature.DetectContentType.
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public async Task Handle_WithUploadedRecognizedImage_CreatesMediaRow()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blobStorage = new FakeBlobStorageService
        {
            BlobInfoToReturn = new UploadedBlobInfo(1024, "image/png", PngHeader),
        };
        var handler = new ConfirmMediaUploadHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();
        var blobName = $"{listingId}/photo.jpg";

        var result = await handler.Handle(new ConfirmMediaUploadCommand(listingId, blobName), CancellationToken.None);

        Assert.Equal("image/png", result.ContentType);
        Assert.Single(dbContext.Photos);
    }

    [Fact]
    public async Task Handle_CalledTwiceForSameBlobName_IsIdempotentAndDoesNotDuplicate()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blobStorage = new FakeBlobStorageService
        {
            BlobInfoToReturn = new UploadedBlobInfo(1024, "image/png", PngHeader),
        };
        var handler = new ConfirmMediaUploadHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();
        var blobName = $"{listingId}/photo.jpg";

        var first = await handler.Handle(new ConfirmMediaUploadCommand(listingId, blobName), CancellationToken.None);
        var second = await handler.Handle(new ConfirmMediaUploadCommand(listingId, blobName), CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
        Assert.Single(dbContext.Photos);
    }

    [Fact]
    public async Task Handle_WhenBlobWasNeverUploaded_ThrowsValidationException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blobStorage = new FakeBlobStorageService { BlobInfoToReturn = null };
        var handler = new ConfirmMediaUploadHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new ConfirmMediaUploadCommand(listingId, $"{listingId}/photo.jpg"), CancellationToken.None));

        Assert.Empty(dbContext.Photos);
    }

    [Fact]
    public async Task Handle_WhenUploadedFileExceedsSizeLimit_ThrowsValidationException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blobStorage = new FakeBlobStorageService
        {
            BlobInfoToReturn = new UploadedBlobInfo(Photo.MaxFileSizeBytes + 1, "image/png", PngHeader),
        };
        var handler = new ConfirmMediaUploadHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new ConfirmMediaUploadCommand(listingId, $"{listingId}/photo.jpg"), CancellationToken.None));

        Assert.Empty(dbContext.Photos);
    }

    [Fact]
    public async Task Handle_WhenUploadedFileIsNotARecognizedImageFormat_ThrowsValidationException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blobStorage = new FakeBlobStorageService
        {
            BlobInfoToReturn = new UploadedBlobInfo(1024, "text/plain", [0x00, 0x01, 0x02]),
        };
        var handler = new ConfirmMediaUploadHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new ConfirmMediaUploadCommand(listingId, $"{listingId}/photo.jpg"), CancellationToken.None));

        Assert.Empty(dbContext.Photos);
    }

    [Fact]
    public async Task Handle_AssignsIncrementingSortOrderPerListing()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blobStorage = new FakeBlobStorageService
        {
            BlobInfoToReturn = new UploadedBlobInfo(1024, "image/png", PngHeader),
        };
        var handler = new ConfirmMediaUploadHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();

        var first = await handler.Handle(new ConfirmMediaUploadCommand(listingId, $"{listingId}/first.jpg"), CancellationToken.None);
        var second = await handler.Handle(new ConfirmMediaUploadCommand(listingId, $"{listingId}/second.jpg"), CancellationToken.None);

        Assert.Equal(0, first.SortOrder);
        Assert.Equal(1, second.SortOrder);
    }

    [Fact]
    public async Task Handle_MakesOnlyTheListingsFirstPhotoPrimary()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blobStorage = new FakeBlobStorageService
        {
            BlobInfoToReturn = new UploadedBlobInfo(1024, "image/png", PngHeader),
        };
        var handler = new ConfirmMediaUploadHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();

        var first = await handler.Handle(new ConfirmMediaUploadCommand(listingId, $"{listingId}/first.jpg"), CancellationToken.None);
        var second = await handler.Handle(new ConfirmMediaUploadCommand(listingId, $"{listingId}/second.jpg"), CancellationToken.None);

        Assert.True(first.IsPrimary);
        Assert.False(second.IsPrimary);
    }
}
