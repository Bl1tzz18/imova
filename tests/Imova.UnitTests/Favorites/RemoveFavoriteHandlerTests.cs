using Imova.Application.Features.Favorites.RemoveFavorite;
using Imova.Domain.Favorites;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Favorites;

public class RemoveFavoriteHandlerTests
{
    [Fact]
    public async Task Handle_ForExistingFavorite_RemovesIt()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        dbContext.Favorites.Add(Favorite.Create(userId, listingId));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new RemoveFavoriteHandler(dbContext);
        await handler.Handle(new RemoveFavoriteCommand(userId, listingId), CancellationToken.None);

        Assert.Empty(dbContext.Favorites);
    }

    [Fact]
    public async Task Handle_ForNonexistentFavorite_IsANoOp()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new RemoveFavoriteHandler(dbContext);

        await handler.Handle(new RemoveFavoriteCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(dbContext.Favorites);
    }

    [Fact]
    public async Task Handle_OnlyRemovesTheMatchingUsersFavorite()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var listingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        dbContext.Favorites.Add(Favorite.Create(userId, listingId));
        dbContext.Favorites.Add(Favorite.Create(otherUserId, listingId));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new RemoveFavoriteHandler(dbContext);
        await handler.Handle(new RemoveFavoriteCommand(userId, listingId), CancellationToken.None);

        var remaining = Assert.Single(dbContext.Favorites);
        Assert.Equal(otherUserId, remaining.UserId);
    }
}
