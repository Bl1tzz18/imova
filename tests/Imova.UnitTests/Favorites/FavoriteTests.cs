using Imova.Domain.Favorites;

namespace Imova.UnitTests.Favorites;

public class FavoriteTests
{
    [Fact]
    public void Create_WithValidData_SetsAllFields()
    {
        var userId = Guid.NewGuid();
        var listingId = Guid.NewGuid();

        var favorite = Favorite.Create(userId, listingId);

        Assert.NotEqual(Guid.Empty, favorite.Id);
        Assert.Equal(userId, favorite.UserId);
        Assert.Equal(listingId, favorite.ListingId);
        Assert.True(favorite.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => Favorite.Create(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Create_WithEmptyListingId_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => Favorite.Create(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void Create_TwiceForSameUserAndProperty_ProducesDistinctIds()
    {
        var userId = Guid.NewGuid();
        var listingId = Guid.NewGuid();

        var first = Favorite.Create(userId, listingId);
        var second = Favorite.Create(userId, listingId);

        Assert.NotEqual(first.Id, second.Id);
    }
}
