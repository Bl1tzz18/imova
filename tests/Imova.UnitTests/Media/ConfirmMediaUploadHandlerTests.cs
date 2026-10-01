using FluentValidation;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common;
using Imova.Application.Features.Media.ConfirmMediaUpload;
using Imova.Application.Features.Media.Sizes;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Media;

public class ConfirmMediaUploadHandlerTests
{
    // Minimal valid PNG signature — see ImageSignature.DetectContentType.
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    // Uploads for a listing id that doesn't exist yet (the add-listing flow) — see MediaAccess.
    private static readonly Guid Uploader = Guid.NewGuid();

    // Every blob a test confirms counts as uploaded, so its sizes can be made from it.
    private static ConfirmMediaUploadHandler NewHandler(
        ImovaDbContext dbContext, FakeBlobStorageService blobStorage, FakePhotoResizer? resizer = null)
    {
        blobStorage.OpenAnyBlob = true;
        return new ConfirmMediaUploadHandler(
            dbContext, blobStorage, new PhotoSizeGenerator(blobStorage, resizer ?? new FakePhotoResizer()),
            NullLogger<ConfirmMediaUploadHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithUploadedRecognizedImage_CreatesMediaRow()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blobStorage = new FakeBlobStorageService
        {
            BlobInfoToReturn = new UploadedBlobInfo(1024, "image/png", PngHeader),
        };
        var handler = NewHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();
        var blobName = $"{listingId}/photo.jpg";

        var result = await handler.Handle(new ConfirmMediaUploadCommand(listingId, blobName, Uploader, false), CancellationToken.None);

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
        var handler = NewHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();
        var blobName = $"{listingId}/photo.jpg";

        var first = await handler.Handle(new ConfirmMediaUploadCommand(listingId, blobName, Uploader, false), CancellationToken.None);
        var second = await handler.Handle(new ConfirmMediaUploadCommand(listingId, blobName, Uploader, false), CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
        Assert.Single(dbContext.Photos);
    }

    [Fact]
    public async Task Handle_WhenBlobWasNeverUploaded_ThrowsValidationException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blobStorage = new FakeBlobStorageService { BlobInfoToReturn = null };
        var handler = NewHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new ConfirmMediaUploadCommand(listingId, $"{listingId}/photo.jpg", Uploader, false), CancellationToken.None));

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
        var handler = NewHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new ConfirmMediaUploadCommand(listingId, $"{listingId}/photo.jpg", Uploader, false), CancellationToken.None));

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
        var handler = NewHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new ConfirmMediaUploadCommand(listingId, $"{listingId}/photo.jpg", Uploader, false), CancellationToken.None));

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
        var handler = NewHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();

        var first = await handler.Handle(new ConfirmMediaUploadCommand(listingId, $"{listingId}/first.jpg", Uploader, false), CancellationToken.None);
        var second = await handler.Handle(new ConfirmMediaUploadCommand(listingId, $"{listingId}/second.jpg", Uploader, false), CancellationToken.None);

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
        var handler = NewHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();

        var first = await handler.Handle(new ConfirmMediaUploadCommand(listingId, $"{listingId}/first.jpg", Uploader, false), CancellationToken.None);
        var second = await handler.Handle(new ConfirmMediaUploadCommand(listingId, $"{listingId}/second.jpg", Uploader, false), CancellationToken.None);

        Assert.True(first.IsPrimary);
        Assert.False(second.IsPrimary);
    }

    [Fact]
    public async Task Handle_MakesTheDisplaySizes_AndTheDtoPointsAtThem()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blobStorage = new FakeBlobStorageService
        {
            BlobInfoToReturn = new UploadedBlobInfo(1024, "image/png", PngHeader),
        };
        var handler = NewHandler(dbContext, blobStorage);
        var listingId = Guid.NewGuid();

        var result = await handler.Handle(new ConfirmMediaUploadCommand(listingId, $"{listingId}/photo.png", Uploader, false), CancellationToken.None);

        Assert.Equal("jpeg-400", System.Text.Encoding.UTF8.GetString(blobStorage.UploadedContent[$"{listingId}/photo_400.jpg"]));
        Assert.Equal("jpeg-800", System.Text.Encoding.UTF8.GetString(blobStorage.UploadedContent[$"{listingId}/photo_800.jpg"]));
        Assert.Equal("jpeg-1600", System.Text.Encoding.UTF8.GetString(blobStorage.UploadedContent[$"{listingId}/photo_1600.jpg"]));
        Assert.Equal("image/jpeg", blobStorage.UploadedContentType);
        Assert.Equal(PhotoSizes.CurrentVersion, Assert.Single(dbContext.Photos).SizesVersion);
        Assert.Equal($"https://blob.test/{listingId}/photo_400.jpg", result.ThumbnailUrl);
        Assert.Equal($"https://blob.test/{listingId}/photo_800.jpg", result.CardUrl);
        Assert.Equal($"https://blob.test/{listingId}/photo_1600.jpg", result.Url);
    }

    [Fact]
    public async Task Handle_WhenTheFileOnlyLooksLikeAnImage_RefusesIt()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blobStorage = new FakeBlobStorageService
        {
            BlobInfoToReturn = new UploadedBlobInfo(1024, "image/png", PngHeader),
        };
        var handler = NewHandler(dbContext, blobStorage, new FakePhotoResizer { Unreadable = true });
        var listingId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new ConfirmMediaUploadCommand(listingId, $"{listingId}/photo.png", Uploader, false), CancellationToken.None));

        Assert.Equal(ErrorCodes.UploadNotAnImage, Assert.Single(ex.Errors).ErrorCode);
        Assert.Empty(dbContext.Photos);
        Assert.Empty(blobStorage.UploadedContent);
    }

    [Fact]
    public async Task Handle_WhenMakingTheSizesFails_KeepsThePhotoAndShowsTheOriginalUntilTheBackfill()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var blobStorage = new FakeBlobStorageService
        {
            BlobInfoToReturn = new UploadedBlobInfo(1024, "image/png", PngHeader),
        };
        var handler = NewHandler(dbContext, blobStorage, new FakePhotoResizer { Throws = true });
        var listingId = Guid.NewGuid();
        var original = $"https://blob.test/{listingId}/photo.png";

        var result = await handler.Handle(new ConfirmMediaUploadCommand(listingId, $"{listingId}/photo.png", Uploader, false), CancellationToken.None);

        Assert.Equal(0, Assert.Single(dbContext.Photos).SizesVersion);
        Assert.Equal(original, result.Url);
        Assert.Equal(original, result.CardUrl);
        Assert.Equal(original, result.ThumbnailUrl);
    }
}
