using Imova.Application.Common.Identity;
using Imova.Application.Features.Properties.GetPropertyById;
using Imova.Domain.Favorites;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class GetPropertyByIdHandlerTests
{
    private static Property AddProperty(ImovaDbContext dbContext, Guid? ownerId = null)
    {
        var property = Property.Create(
            ownerId ?? Guid.NewGuid(), "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        dbContext.Properties.Add(property);
        return property;
    }

    [Fact]
    public async Task Handle_ForExistingProperty_ReturnsDto()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertyByIdQuery(property.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(property.Id, result!.Id);
    }

    [Fact]
    public async Task Handle_ForUnknownId_ReturnsNull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());

        var result = await handler.Handle(new GetPropertyByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_IncludesOwnerContactInfo()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var owner = new ApplicationUser { Id = Guid.NewGuid(), Email = "owner@example.com", PhoneNumber = "+373 69 123 456" };
        dbContext.Users.Add(owner);
        var property = AddProperty(dbContext, owner.Id);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertyByIdQuery(property.Id), CancellationToken.None);

        Assert.NotNull(result!.Owner);
        Assert.Equal("owner@example.com", result.Owner!.Email);
        Assert.Equal("+373 69 123 456", result.Owner.Phone);
    }

    [Fact]
    public async Task Handle_WithoutCurrentUserId_IsSavedIsFalse()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertyByIdQuery(property.Id), CancellationToken.None);

        Assert.False(result!.IsSaved);
    }

    [Fact]
    public async Task Handle_WithCurrentUserIdWhoSavedIt_IsSavedIsTrue()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext);
        var currentUserId = Guid.NewGuid();
        dbContext.Favorites.Add(Favorite.Create(currentUserId, property.Id));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertyByIdQuery(property.Id, currentUserId), CancellationToken.None);

        Assert.True(result!.IsSaved);
    }

    [Fact]
    public async Task Handle_WithCurrentUserIdWhoDidNotSaveIt_IsSavedIsFalse()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertyByIdQuery(property.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result!.IsSaved);
    }
}
