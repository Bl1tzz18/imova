using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Properties.DeleteProperty;
using Imova.Domain.Favorites;
using Imova.Domain.Locations;
using Imova.Domain.Media;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class DeletePropertyHandlerTests
{
    private static Property AddProperty(ImovaDbContext dbContext, Guid ownerId)
    {
        var property = Property.Create(
            ownerId, "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        dbContext.Properties.Add(property);
        return property;
    }

    [Fact]
    public async Task Handle_ByOwner_RemovesPropertyAndReturnsTrue()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new DeletePropertyHandler(dbContext);
        var result = await handler.Handle(new DeletePropertyCommand(property.Id, ownerId, false), CancellationToken.None);

        Assert.True(result);
        Assert.Empty(dbContext.Properties);
    }

    [Fact]
    public async Task Handle_ByOwner_AlsoRemovesLocationMediaAndFavorites()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        dbContext.PropertyLocations.Add(PropertyLocation.Create(property.Id, "Moldova", "Chisinau", null, 47.0105, 28.8638));
        dbContext.PropertyMedias.Add(PropertyMedia.Create(property.Id, $"{property.Id}/photo.jpg", "image/jpeg", 1024));
        dbContext.Favorites.Add(Favorite.Create(Guid.NewGuid(), property.Id));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new DeletePropertyHandler(dbContext);
        await handler.Handle(new DeletePropertyCommand(property.Id, ownerId, false), CancellationToken.None);

        Assert.Empty(dbContext.PropertyLocations);
        Assert.Empty(dbContext.PropertyMedias);
        Assert.Empty(dbContext.Favorites);
    }

    [Fact]
    public async Task Handle_ByNonOwnerNonAdmin_ThrowsForbiddenAccessExceptionAndLeavesPropertyIntact()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new DeletePropertyHandler(dbContext);
        var otherUserId = Guid.NewGuid();

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new DeletePropertyCommand(property.Id, otherUserId, false), CancellationToken.None));

        Assert.Single(dbContext.Properties);
    }

    [Fact]
    public async Task Handle_ByAdminNonOwner_Succeeds()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new DeletePropertyHandler(dbContext);
        var adminId = Guid.NewGuid();

        var result = await handler.Handle(new DeletePropertyCommand(property.Id, adminId, true), CancellationToken.None);

        Assert.True(result);
        Assert.Empty(dbContext.Properties);
    }

    [Fact]
    public async Task Handle_ForUnknownPropertyId_ReturnsFalse()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new DeletePropertyHandler(dbContext);

        var result = await handler.Handle(new DeletePropertyCommand(Guid.NewGuid(), Guid.NewGuid(), false), CancellationToken.None);

        Assert.False(result);
    }
}
