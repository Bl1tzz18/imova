using Imova.Application.Features.Favorites.SaveFavorite;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Favorites;

public class SaveFavoriteHandlerTests
{
    private static Property AddProperty(ImovaDbContext dbContext)
    {
        var property = Property.Create(
            Guid.NewGuid(), "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        dbContext.Properties.Add(property);
        return property;
    }

    [Fact]
    public async Task Handle_ForExistingProperty_CreatesFavoriteAndReturnsTrue()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        var userId = Guid.NewGuid();

        var handler = new SaveFavoriteHandler(dbContext);
        var result = await handler.Handle(new SaveFavoriteCommand(userId, property.Id), CancellationToken.None);

        Assert.True(result);
        var favorite = Assert.Single(dbContext.Favorites);
        Assert.Equal(userId, favorite.UserId);
        Assert.Equal(property.Id, favorite.PropertyId);
    }

    [Fact]
    public async Task Handle_CalledTwiceForSameUserAndProperty_IsIdempotent()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        var userId = Guid.NewGuid();
        var handler = new SaveFavoriteHandler(dbContext);

        await handler.Handle(new SaveFavoriteCommand(userId, property.Id), CancellationToken.None);
        var secondResult = await handler.Handle(new SaveFavoriteCommand(userId, property.Id), CancellationToken.None);

        Assert.True(secondResult);
        Assert.Single(dbContext.Favorites);
    }

    [Fact]
    public async Task Handle_ForNonexistentProperty_ReturnsFalseAndCreatesNoFavorite()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new SaveFavoriteHandler(dbContext);

        var result = await handler.Handle(new SaveFavoriteCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result);
        Assert.Empty(dbContext.Favorites);
    }
}
