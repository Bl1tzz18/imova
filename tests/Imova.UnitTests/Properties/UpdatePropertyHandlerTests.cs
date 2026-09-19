using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Properties.UpdateProperty;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class UpdatePropertyHandlerTests
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
    public async Task Handle_ByOwner_UpdatesAndReturnsDto()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdatePropertyHandler(dbContext);
        var result = await handler.Handle(
            new UpdatePropertyCommand(property.Id, ownerId, false, "Titlu nou", "Descriere noua", 600m),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Titlu nou", result!.Title);
        Assert.Equal(600m, result.Price);
    }

    [Fact]
    public async Task Handle_ByNonOwnerNonAdmin_ThrowsForbiddenAccessException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdatePropertyHandler(dbContext);
        var otherUserId = Guid.NewGuid();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new UpdatePropertyCommand(property.Id, otherUserId, false, "Hijacked", "Descriere", 1m),
            CancellationToken.None));

        // The listing must be left untouched.
        Assert.Equal("Titlu", property.Title);
    }

    [Fact]
    public async Task Handle_ByAdminNonOwner_Succeeds()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdatePropertyHandler(dbContext);
        var adminId = Guid.NewGuid();

        var result = await handler.Handle(
            new UpdatePropertyCommand(property.Id, adminId, true, "Updated by admin", "Descriere", 700m),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Updated by admin", result!.Title);
    }

    [Fact]
    public async Task Handle_ForUnknownPropertyId_ReturnsNull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new UpdatePropertyHandler(dbContext);

        var result = await handler.Handle(
            new UpdatePropertyCommand(Guid.NewGuid(), Guid.NewGuid(), false, "Titlu", "Descriere", 100m),
            CancellationToken.None);

        Assert.Null(result);
    }
}
