using Imova.Application.Features.Properties.CreateProperty;
using Imova.Domain.Properties;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class CreatePropertyHandlerTests
{
    private static CreatePropertyCommand ValidCommand(Guid? id = null, Guid? ownerId = null) => new(
        id,
        ownerId ?? Guid.NewGuid(),
        "Apartament 2 camere",
        "Apartament luminos, aproape de centru.",
        PropertyType.Apartment,
        ListingType.Rent,
        550m,
        "EUR",
        "Moldova",
        "Chisinau",
        "Botanica",
        47.0105,
        28.8638,
        54m,
        2m,
        null,
        3,
        9,
        null,
        null,
        null,
        null);

    [Fact]
    public async Task Handle_WithValidCommand_PersistsPropertyAndLocationAndReturnsDto()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreatePropertyHandler(dbContext);
        var ownerId = Guid.NewGuid();

        var result = await handler.Handle(ValidCommand(ownerId: ownerId), CancellationToken.None);

        Assert.Equal(ownerId, result.OwnerId);
        // A new listing goes straight into the admin review queue — see
        // CreatePropertyHandler's call to SubmitForReview().
        Assert.Equal("PendingReview", result.Status);
        Assert.NotNull(result.Location);
        Assert.Equal("Chisinau", result.Location!.City);

        Assert.Single(dbContext.Properties);
        Assert.Single(dbContext.PropertyLocations);
        Assert.Equal(result.Id, dbContext.PropertyLocations.Single().PropertyId);
    }

    [Fact]
    public async Task Handle_WithClientSuppliedId_UsesThatIdForThePropertyAndLocation()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreatePropertyHandler(dbContext);
        var suppliedId = Guid.NewGuid();

        var result = await handler.Handle(ValidCommand(id: suppliedId), CancellationToken.None);

        Assert.Equal(suppliedId, result.Id);
        Assert.Equal(suppliedId, dbContext.PropertyLocations.Single().PropertyId);
    }

    [Fact]
    public async Task Handle_WithoutClientSuppliedId_GeneratesANewId()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreatePropertyHandler(dbContext);

        var result = await handler.Handle(ValidCommand(id: null), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
    }
}
