using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Media.ConfirmMediaUpload;
using Imova.Application.Features.Media.DeleteMedia;
using Imova.Application.Features.Media.RequestUploadUrl;
using Imova.Application.Features.Media.Sizes;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Media;

// Who may add photos under a listing id (MediaAccess), through the upload-url / confirm / delete
// handlers: the owner of an existing listing, or — before the listing exists — its first uploader.
public class MediaAccessTests
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly ImovaDbContext _dbContext = TestDbContextFactory.Create();
    private readonly FakeBlobStorageService _blobs = new() { BlobInfoToReturn = new UploadedBlobInfo(1024, "image/png", PngHeader) };
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _strangerId = Guid.NewGuid();
    private readonly Listing _listing;

    public MediaAccessTests()
    {
        var publisher = ListingTestData.AddIndividualPublisher(_dbContext, _ownerId);
        _listing = ListingTestData.AddListing(_dbContext, publisher.Id);
        _dbContext.SaveChanges();
    }

    private Task RequestUrlAsync(Guid listingId, Guid userId, bool isAdmin = false) =>
        new RequestUploadUrlHandler(_dbContext, _blobs)
            .Handle(new RequestUploadUrlCommand(listingId, ".jpg", userId, isAdmin), CancellationToken.None);

    private Task ConfirmAsync(Guid listingId, Guid userId, bool isAdmin = false)
    {
        var blobName = $"{listingId}/{Guid.NewGuid()}.jpg";
        _blobs.StoredBlobNames.Add(blobName);
        return new ConfirmMediaUploadHandler(
                _dbContext, _blobs, new PhotoSizeGenerator(_blobs, new FakePhotoResizer()), NullLogger<ConfirmMediaUploadHandler>.Instance)
            .Handle(new ConfirmMediaUploadCommand(listingId, blobName, userId, isAdmin), CancellationToken.None);
    }

    [Fact]
    public async Task ExistingListing_TheOwnerAndAnAdminMayUpload()
    {
        await RequestUrlAsync(_listing.Id, _ownerId);
        await ConfirmAsync(_listing.Id, _ownerId);
        await RequestUrlAsync(_listing.Id, _strangerId, isAdmin: true);

        Assert.Single(_dbContext.Photos);
    }

    [Fact]
    public async Task ExistingListing_SomeoneElseMayNotUpload()
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => RequestUrlAsync(_listing.Id, _strangerId));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => ConfirmAsync(_listing.Id, _strangerId));

        Assert.Empty(_dbContext.Photos);
    }

    [Fact]
    public async Task ListingNotCreatedYet_TheFirstUploaderClaimsTheId()
    {
        var pendingId = Guid.NewGuid();

        await RequestUrlAsync(pendingId, _ownerId);
        await ConfirmAsync(pendingId, _ownerId);
        await ConfirmAsync(pendingId, _ownerId);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => RequestUrlAsync(pendingId, _strangerId));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => ConfirmAsync(pendingId, _strangerId));
        Assert.Equal(2, _dbContext.Photos.Count());
    }

    [Fact]
    public async Task Confirm_RecordsWhoUploadedThePhoto()
    {
        await ConfirmAsync(_listing.Id, _ownerId);

        Assert.Equal(_ownerId, Assert.Single(_dbContext.Photos).UploadedByUserId);
    }

    [Fact]
    public async Task ListingNotCreatedYet_TheUploaderCanDeleteTheirPhoto_NobodyElseCan()
    {
        // The add-listing form removes a photo right away — the listing doesn't exist yet.
        var pendingId = Guid.NewGuid();
        await ConfirmAsync(pendingId, _ownerId);
        var photo = Assert.Single(_dbContext.Photos);
        var handler = new DeleteMediaHandler(_dbContext, _blobs);

        Assert.False(await handler.Handle(new DeleteMediaCommand(pendingId, photo.Id, _strangerId, false), CancellationToken.None));
        Assert.Single(_dbContext.Photos);

        Assert.True(await handler.Handle(new DeleteMediaCommand(pendingId, photo.Id, _ownerId, false), CancellationToken.None));
        Assert.Empty(_dbContext.Photos);
        Assert.Contains(photo.BlobName, _blobs.DeletedBlobNames);
    }
}
