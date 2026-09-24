using Imova.Application.Features.Favorites.GetFavorites;
using Imova.Domain.Favorites;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Favorites;

public class GetFavoritesHandlerTests
{
    private static Listing AddListing(ImovaDbContext dbContext) =>
        ListingTestData.AddListing(dbContext, ListingTestData.AddIndividualPublisher(dbContext).Id);

    [Fact]
    public async Task Handle_WithNoFavorites_ReturnsEmptyList()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetFavoritesHandler(dbContext, new FakeBlobStorageService());

        var result = await handler.Handle(new GetFavoritesQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyTheRequestedUsersSavedListingsAllMarkedIsSaved()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var savedByUser = AddListing(dbContext);
        var savedByOther = AddListing(dbContext);
        dbContext.Favorites.Add(Favorite.Create(userId, savedByUser.Id));
        dbContext.Favorites.Add(Favorite.Create(otherUserId, savedByOther.Id));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetFavoritesHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetFavoritesQuery(userId), CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(savedByUser.Id, dto.Id);
        Assert.True(dto.IsSaved);
    }

    [Fact]
    public async Task Handle_OrdersMostRecentlySavedFirst()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var first = AddListing(dbContext);
        var second = AddListing(dbContext);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        dbContext.Favorites.Add(Favorite.Create(userId, first.Id));
        await dbContext.SaveChangesAsync(CancellationToken.None);
        dbContext.Favorites.Add(Favorite.Create(userId, second.Id));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetFavoritesHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetFavoritesQuery(userId), CancellationToken.None);

        Assert.Equal([second.Id, first.Id], result.Select(p => p.Id));
    }

    [Fact]
    public async Task Handle_SkipsFavoritesWhoseListingWasDeleted()
    {
        // A stale Favorite row pointing at a deleted listing shouldn't happen (DeleteListingHandler
        // cleans these up), but the handler must not blow up if one somehow exists.
        await using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        dbContext.Favorites.Add(Favorite.Create(userId, Guid.NewGuid()));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetFavoritesHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetFavoritesQuery(userId), CancellationToken.None);

        Assert.Empty(result);
    }
}
