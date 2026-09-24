using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Media.DeleteMedia;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Media;

public class DeleteMediaHandlerTests
{
    private static (Listing Listing, Guid OwnerId) AddListing(ImovaDbContext dbContext)
    {
        var ownerId = Guid.NewGuid();
        var publisher = ListingTestData.AddIndividualPublisher(dbContext, ownerId);
        return (ListingTestData.AddListing(dbContext, publisher.Id), ownerId);
    }

    private static Photo AddPhoto(ImovaDbContext dbContext, Guid listingId, int sortOrder = 0, bool isPrimary = false)
    {
        var photo = Photo.Create(listingId, $"{listingId}/{Guid.NewGuid()}.jpg", "image/jpeg", 1024, sortOrder, isPrimary);
        dbContext.Photos.Add(photo);
        return photo;
    }

    [Fact]
    public async Task Handle_ByOwner_RemovesPhotoRowAndDeletesBlobAndReturnsTrue()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (listing, ownerId) = AddListing(dbContext);
        var photo = AddPhoto(dbContext, listing.Id);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var blobStorage = new FakeBlobStorageService();
        var handler = new DeleteMediaHandler(dbContext, blobStorage);

        var result = await handler.Handle(new DeleteMediaCommand(listing.Id, photo.Id, ownerId, false), CancellationToken.None);

        Assert.True(result);
        Assert.Empty(dbContext.Photos);
        Assert.Contains(photo.BlobName, blobStorage.DeletedBlobNames);
    }

    [Fact]
    public async Task Handle_ByOwnerViaTheirAgencyPublisher_Succeeds()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var agency = ListingTestData.AddAgencyPublisher(dbContext, ownerId);
        var listing = ListingTestData.AddListing(dbContext, agency.Id);
        var photo = AddPhoto(dbContext, listing.Id);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteMediaHandler(dbContext, new FakeBlobStorageService());

        Assert.True(await handler.Handle(new DeleteMediaCommand(listing.Id, photo.Id, ownerId, false), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ByNonOwnerNonAdmin_ThrowsForbiddenAccessExceptionAndLeavesPhotoIntact()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (listing, _) = AddListing(dbContext);
        var photo = AddPhoto(dbContext, listing.Id);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteMediaHandler(dbContext, new FakeBlobStorageService());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new DeleteMediaCommand(listing.Id, photo.Id, Guid.NewGuid(), false), CancellationToken.None));

        Assert.Single(dbContext.Photos);
    }

    [Fact]
    public async Task Handle_ByAdminNonOwner_Succeeds()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (listing, _) = AddListing(dbContext);
        var photo = AddPhoto(dbContext, listing.Id);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteMediaHandler(dbContext, new FakeBlobStorageService());

        var result = await handler.Handle(new DeleteMediaCommand(listing.Id, photo.Id, Guid.NewGuid(), true), CancellationToken.None);

        Assert.True(result);
        Assert.Empty(dbContext.Photos);
    }

    [Fact]
    public async Task Handle_DeletingThePrimaryPhoto_PromotesTheNextOneInOrder()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (listing, ownerId) = AddListing(dbContext);
        var cover = AddPhoto(dbContext, listing.Id, sortOrder: 0, isPrimary: true);
        var third = AddPhoto(dbContext, listing.Id, sortOrder: 2);
        var second = AddPhoto(dbContext, listing.Id, sortOrder: 1);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteMediaHandler(dbContext, new FakeBlobStorageService());
        await handler.Handle(new DeleteMediaCommand(listing.Id, cover.Id, ownerId, false), CancellationToken.None);

        Assert.True(dbContext.Photos.Single(p => p.Id == second.Id).IsPrimary);
        Assert.False(dbContext.Photos.Single(p => p.Id == third.Id).IsPrimary);
    }

    [Fact]
    public async Task Handle_ForUnknownMediaId_ReturnsFalse()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (listing, ownerId) = AddListing(dbContext);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteMediaHandler(dbContext, new FakeBlobStorageService());

        var result = await handler.Handle(
            new DeleteMediaCommand(listing.Id, Guid.NewGuid(), ownerId, false), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_ForPhotoBelongingToDifferentListing_ReturnsFalse()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (listing, ownerId) = AddListing(dbContext);
        var (otherListing, _) = AddListing(dbContext);
        var photo = AddPhoto(dbContext, otherListing.Id);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteMediaHandler(dbContext, new FakeBlobStorageService());

        var result = await handler.Handle(
            new DeleteMediaCommand(listing.Id, photo.Id, ownerId, false), CancellationToken.None);

        Assert.False(result);
        Assert.Single(dbContext.Photos);
    }
}
