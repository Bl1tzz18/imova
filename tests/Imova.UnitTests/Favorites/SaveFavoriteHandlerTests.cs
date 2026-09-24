using Imova.Application.Features.Favorites.SaveFavorite;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Favorites;

public class SaveFavoriteHandlerTests
{
    private static Listing AddListing(ImovaDbContext dbContext) =>
        ListingTestData.AddListing(dbContext, ListingTestData.AddIndividualPublisher(dbContext).Id);

    [Fact]
    public async Task Handle_ForExistingListing_CreatesFavoriteAndReturnsTrue()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var listing = AddListing(dbContext);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        var userId = Guid.NewGuid();

        var handler = new SaveFavoriteHandler(dbContext);
        var result = await handler.Handle(new SaveFavoriteCommand(userId, listing.Id), CancellationToken.None);

        Assert.True(result);
        var favorite = Assert.Single(dbContext.Favorites);
        Assert.Equal(userId, favorite.UserId);
        Assert.Equal(listing.Id, favorite.ListingId);
    }

    [Fact]
    public async Task Handle_CalledTwiceForSameUserAndListing_IsIdempotent()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var listing = AddListing(dbContext);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        var userId = Guid.NewGuid();
        var handler = new SaveFavoriteHandler(dbContext);

        await handler.Handle(new SaveFavoriteCommand(userId, listing.Id), CancellationToken.None);
        var secondResult = await handler.Handle(new SaveFavoriteCommand(userId, listing.Id), CancellationToken.None);

        Assert.True(secondResult);
        Assert.Single(dbContext.Favorites);
    }

    [Fact]
    public async Task Handle_ForNonexistentListing_ReturnsFalseAndCreatesNoFavorite()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new SaveFavoriteHandler(dbContext);

        var result = await handler.Handle(new SaveFavoriteCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result);
        Assert.Empty(dbContext.Favorites);
    }
}
