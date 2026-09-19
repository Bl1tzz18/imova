using Imova.Application.Features.Favorites.GetFavorites;
using Imova.Domain.Favorites;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Favorites;

public class GetFavoritesHandlerTests
{
    private static Property AddProperty(ImovaDbContext dbContext, string title)
    {
        var property = Property.Create(
            Guid.NewGuid(), title, "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        dbContext.Properties.Add(property);
        return property;
    }

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
        var savedByUser = AddProperty(dbContext, "Saved by user");
        var savedByOther = AddProperty(dbContext, "Saved by other");
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
        var first = AddProperty(dbContext, "First saved");
        var second = AddProperty(dbContext, "Second saved");
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
        // A stale Favorite row pointing at a deleted property shouldn't happen (DeletePropertyHandler
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
