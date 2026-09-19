using Imova.Application.Features.Properties.GetProperties;
using Imova.Domain.Favorites;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class GetPropertiesHandlerTests
{
    private static Property AddProperty(
        ImovaDbContext dbContext,
        PropertyType propertyType = PropertyType.Apartment,
        decimal? area = 54m,
        decimal? rooms = 2m,
        short? floor = 3,
        short? totalFloors = 9)
    {
        var property = Property.Create(
            Guid.NewGuid(), "Titlu", "Descriere", propertyType, ListingType.Rent, 550m, "EUR",
            area, rooms, null, floor, totalFloors);
        dbContext.Properties.Add(property);
        return property;
    }

    [Fact]
    public async Task Handle_WithNoFilter_ReturnsAllProperties()
    {
        await using var dbContext = TestDbContextFactory.Create();
        AddProperty(dbContext);
        AddProperty(dbContext, PropertyType.House, rooms: null, floor: null);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertiesHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertiesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Handle_WithPropertyTypeFilter_ReturnsOnlyMatchingProperties()
    {
        await using var dbContext = TestDbContextFactory.Create();
        AddProperty(dbContext, PropertyType.Apartment);
        AddProperty(dbContext, PropertyType.House, rooms: null, floor: null);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertiesHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertiesQuery(PropertyType.Apartment), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Apartment", result[0].PropertyType);
    }

    [Fact]
    public async Task Handle_WithoutCurrentUserId_AllPropertiesHaveIsSavedFalse()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext);
        var someoneElse = Guid.NewGuid();
        dbContext.Favorites.Add(Favorite.Create(someoneElse, property.Id));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertiesHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertiesQuery(), CancellationToken.None);

        Assert.False(result.Single().IsSaved);
    }

    [Fact]
    public async Task Handle_WithCurrentUserIdWhoSavedTheListing_MarksIsSavedTrue()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext);
        var currentUserId = Guid.NewGuid();
        dbContext.Favorites.Add(Favorite.Create(currentUserId, property.Id));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertiesHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertiesQuery(CurrentUserId: currentUserId), CancellationToken.None);

        Assert.True(result.Single().IsSaved);
    }

    [Fact]
    public async Task Handle_WithCurrentUserIdWhoDidNotSaveTheListing_MarksIsSavedFalse()
    {
        await using var dbContext = TestDbContextFactory.Create();
        AddProperty(dbContext);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertiesHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertiesQuery(CurrentUserId: Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Single().IsSaved);
    }

    [Fact]
    public async Task Handle_IncludesLocationWhenOneExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext);
        var location = PropertyLocation.Create(property.Id, "Moldova", "Chisinau", "Botanica", 47.0105, 28.8638);
        dbContext.PropertyLocations.Add(location);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertiesHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertiesQuery(), CancellationToken.None);

        Assert.NotNull(result.Single().Location);
        Assert.Equal("Chisinau", result.Single().Location!.City);
    }

    [Fact]
    public async Task Handle_WithNoProperties_ReturnsEmptyList()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetPropertiesHandler(dbContext, new FakeBlobStorageService());

        var result = await handler.Handle(new GetPropertiesQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}
