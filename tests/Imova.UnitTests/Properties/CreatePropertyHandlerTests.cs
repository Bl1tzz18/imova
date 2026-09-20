using Imova.Application.Features.Properties.CreateProperty;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class CreatePropertyHandlerTests
{
    private static (Raion Raion, Localitate Localitate) SeedRaionAndLocalitate(Imova.Infrastructure.ImovaDbContext dbContext)
    {
        var raion = Raion.Create(Guid.NewGuid(), "0100", "Chisinau", null, LocalityLabel.Sector);
        var localitate = Localitate.Create(Guid.NewGuid(), raion.Id, null, "0101", "Botanica", null);
        dbContext.Raioane.Add(raion);
        dbContext.Localitati.Add(localitate);
        dbContext.SaveChanges();
        return (raion, localitate);
    }

    private static CreatePropertyCommand ValidCommand(
        Guid raionId,
        Guid? localitateId,
        Guid? id = null,
        Guid? ownerId = null,
        string? streetAddress = null) => new(
        id,
        ownerId ?? Guid.NewGuid(),
        "Apartament 2 camere",
        "Apartament luminos, aproape de centru.",
        PropertyType.Apartment,
        ListingType.Rent,
        550m,
        "EUR",
        "Moldova",
        raionId,
        localitateId,
        streetAddress,
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
        var (raion, localitate) = SeedRaionAndLocalitate(dbContext);
        var handler = new CreatePropertyHandler(dbContext, new FakeGeocodingService());
        var ownerId = Guid.NewGuid();

        var result = await handler.Handle(
            ValidCommand(raion.Id, localitate.Id, ownerId: ownerId), CancellationToken.None);

        Assert.Equal(ownerId, result.OwnerId);
        // A new listing goes straight into the admin review queue — see
        // CreatePropertyHandler's call to SubmitForReview().
        Assert.Equal("PendingReview", result.Status);
        Assert.NotNull(result.Location);
        Assert.Equal("Chisinau", result.Location!.RaionName);
        Assert.Equal("Botanica", result.Location.LocalitateName);

        Assert.Single(dbContext.Properties);
        Assert.Single(dbContext.PropertyLocations);
        Assert.Equal(result.Id, dbContext.PropertyLocations.Single().PropertyId);
    }

    [Fact]
    public async Task Handle_WithClientSuppliedId_UsesThatIdForThePropertyAndLocation()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (raion, localitate) = SeedRaionAndLocalitate(dbContext);
        var handler = new CreatePropertyHandler(dbContext, new FakeGeocodingService());
        var suppliedId = Guid.NewGuid();

        var result = await handler.Handle(
            ValidCommand(raion.Id, localitate.Id, id: suppliedId), CancellationToken.None);

        Assert.Equal(suppliedId, result.Id);
        Assert.Equal(suppliedId, dbContext.PropertyLocations.Single().PropertyId);
    }

    [Fact]
    public async Task Handle_WithoutClientSuppliedId_GeneratesANewId()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (raion, localitate) = SeedRaionAndLocalitate(dbContext);
        var handler = new CreatePropertyHandler(dbContext, new FakeGeocodingService());

        var result = await handler.Handle(
            ValidCommand(raion.Id, localitate.Id, id: null), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task Handle_WhenGeocodingResolves_StoresReturnedCoordinates()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (raion, localitate) = SeedRaionAndLocalitate(dbContext);
        var geocodingService = new FakeGeocodingService
        {
            ResultToReturn = new(47.75, 27.9167, "Balti, Moldova"),
        };
        var handler = new CreatePropertyHandler(dbContext, geocodingService);

        var result = await handler.Handle(ValidCommand(raion.Id, localitate.Id), CancellationToken.None);

        Assert.Equal(47.75, result.Location!.Latitude);
        Assert.Equal(27.9167, result.Location.Longitude);
        Assert.Equal("Botanica, Chisinau, Moldova", geocodingService.LastAddressRequested);
    }

    [Fact]
    public async Task Handle_WhenGeocodingFails_StillPersistsPropertyWithNullCoordinates()
    {
        // Geocoding never throws (see IGeocodingService) — a null result must not block listing
        // creation, it just means the listing saves without coordinates.
        await using var dbContext = TestDbContextFactory.Create();
        var (raion, localitate) = SeedRaionAndLocalitate(dbContext);
        var geocodingService = new FakeGeocodingService { ResultToReturn = null };
        var handler = new CreatePropertyHandler(dbContext, geocodingService);

        var result = await handler.Handle(ValidCommand(raion.Id, localitate.Id), CancellationToken.None);

        Assert.NotNull(result.Location);
        Assert.Null(result.Location!.Latitude);
        Assert.Null(result.Location.Longitude);
        Assert.Single(dbContext.Properties);
    }

    [Fact]
    public async Task Handle_WithStreetAddress_IncludesItInTheGeocodedAddressAndPersistsIt()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var (raion, localitate) = SeedRaionAndLocalitate(dbContext);
        var geocodingService = new FakeGeocodingService
        {
            ResultToReturn = new(47.0105, 28.8638, "Str. Ismail 44, Botanica, Chisinau, Moldova"),
        };
        var handler = new CreatePropertyHandler(dbContext, geocodingService);

        var result = await handler.Handle(
            ValidCommand(raion.Id, localitate.Id, streetAddress: "Str. Ismail 44"), CancellationToken.None);

        Assert.Equal("Str. Ismail 44, Botanica, Chisinau, Moldova", geocodingService.LastAddressRequested);
        Assert.Equal("Str. Ismail 44", result.Location!.Street);
    }
}
