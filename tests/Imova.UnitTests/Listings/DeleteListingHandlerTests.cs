using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Listings.DeleteListing;
using Imova.Domain.Favorites;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class DeleteListingHandlerTests
{
    private readonly ImovaDbContext _dbContext = TestDbContextFactory.Create();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _publisherId;

    public DeleteListingHandlerTests()
    {
        _publisherId = ListingTestData.AddIndividualPublisher(_dbContext, _ownerId).Id;
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task Handle_ByOwner_RemovesListingPhotosFavoritesAndTheNowUnlistedProperty()
    {
        var listing = ListingTestData.AddListing(_dbContext, _publisherId);
        _dbContext.Photos.Add(Photo.Create(listing.Id, $"{listing.Id}/a.jpg", "image/jpeg", 100));
        _dbContext.Favorites.Add(Favorite.Create(Guid.NewGuid(), listing.Id));
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await new DeleteListingHandler(_dbContext).Handle(
            new DeleteListingCommand(listing.Id, _ownerId, false), CancellationToken.None);

        Assert.True(result);
        Assert.Empty(_dbContext.Listings);
        Assert.Empty(_dbContext.Photos);
        Assert.Empty(_dbContext.Favorites);
        Assert.Empty(_dbContext.Properties);
        Assert.Empty(_dbContext.PropertyLocations);
    }

    [Fact]
    public async Task Handle_WhenThePropertyHasAnotherListing_KeepsThePropertyAndLocation()
    {
        var first = ListingTestData.AddListing(_dbContext, _publisherId);
        var relisting = ListingTestData.NewListing(first.PropertyId, _publisherId, TransactionType.Sale);
        _dbContext.Listings.Add(relisting);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        await new DeleteListingHandler(_dbContext).Handle(new DeleteListingCommand(first.Id, _ownerId, false), CancellationToken.None);

        Assert.Equal(relisting.Id, Assert.Single(_dbContext.Listings).Id);
        Assert.Single(_dbContext.Properties);
        Assert.Single(_dbContext.PropertyLocations);
    }

    [Fact]
    public async Task Handle_ByNonOwnerNonAdmin_ThrowsForbiddenAndLeavesEverythingIntact()
    {
        var listing = ListingTestData.AddListing(_dbContext, _publisherId);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => new DeleteListingHandler(_dbContext).Handle(
            new DeleteListingCommand(listing.Id, Guid.NewGuid(), false), CancellationToken.None));

        Assert.Single(_dbContext.Listings);
        Assert.Single(_dbContext.Properties);
    }

    [Fact]
    public async Task Handle_ByAdminNonOwner_Succeeds()
    {
        var listing = ListingTestData.AddListing(_dbContext, _publisherId);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.True(await new DeleteListingHandler(_dbContext).Handle(
            new DeleteListingCommand(listing.Id, Guid.NewGuid(), true), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForUnknownListingId_ReturnsFalse()
    {
        Assert.False(await new DeleteListingHandler(_dbContext).Handle(
            new DeleteListingCommand(Guid.NewGuid(), _ownerId, false), CancellationToken.None));
    }
}
