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

    // Mirrors AddProperty's Apartment/Rent fixture by default, overridable per test — the command
    // now carries every field CreatePropertyCommand does (see UpdatePropertyCommand), not just
    // title/description/price.
    private static UpdatePropertyCommand BuildCommand(
        Guid propertyId,
        Guid requestingUserId,
        bool isAdmin,
        string title = "Titlu nou",
        string description = "Descriere noua",
        decimal price = 600m,
        PropertyType propertyType = PropertyType.Apartment,
        ListingType listingType = ListingType.Rent,
        string currency = "EUR",
        string country = "Moldova",
        string city = "Chisinau",
        string? district = null,
        string? streetAddress = null,
        decimal? area = 54m,
        decimal? rooms = 2m,
        short? bathrooms = null,
        short? floor = 3,
        short? totalFloors = 9,
        short? yearBuilt = null,
        bool? furnished = null,
        bool? parkingAvailable = null,
        bool? petsAllowed = null) =>
        new(
            propertyId, requestingUserId, isAdmin, title, description, propertyType, listingType, price,
            currency, country, city, district, streetAddress, area, rooms, bathrooms, floor,
            totalFloors, yearBuilt, furnished, parkingAvailable, petsAllowed);

    [Fact]
    public async Task Handle_ByOwner_UpdatesAndReturnsDto()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdatePropertyHandler(dbContext, new FakeGeocodingService());
        var result = await handler.Handle(
            BuildCommand(property.Id, ownerId, false, title: "Titlu nou", description: "Descriere noua", price: 600m),
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

        var handler = new UpdatePropertyHandler(dbContext, new FakeGeocodingService());
        var otherUserId = Guid.NewGuid();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            BuildCommand(property.Id, otherUserId, false, title: "Hijacked", description: "Descriere", price: 1m),
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

        var handler = new UpdatePropertyHandler(dbContext, new FakeGeocodingService());
        var adminId = Guid.NewGuid();

        var result = await handler.Handle(
            BuildCommand(property.Id, adminId, true, title: "Updated by admin", description: "Descriere", price: 700m),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Updated by admin", result!.Title);
    }

    [Fact]
    public async Task Handle_ForUnknownPropertyId_ReturnsNull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new UpdatePropertyHandler(dbContext, new FakeGeocodingService());

        var result = await handler.Handle(
            BuildCommand(Guid.NewGuid(), Guid.NewGuid(), false, title: "Titlu", description: "Descriere", price: 100m),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ForRejectedListing_ResubmitsForReview()
    {
        // The frontend no longer offers a separate "submit for review" button for Rejected
        // listings — saving the edit is what resubmits it (see OwnerListingsList.tsx /
        // PropertyForm.tsx). This is the handler-side half of that behavior.
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        property.SubmitForReview();
        property.Reject("Missing photos");
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdatePropertyHandler(dbContext, new FakeGeocodingService());
        var result = await handler.Handle(
            BuildCommand(property.Id, ownerId, false, title: "Titlu corectat", description: "Descriere corectata", price: 600m),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("PendingReview", result!.Status);
        Assert.Null(result.RejectionReason);
    }

    [Fact]
    public async Task Handle_ForNonRejectedListing_DoesNotChangeStatus()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdatePropertyHandler(dbContext, new FakeGeocodingService());
        var result = await handler.Handle(
            BuildCommand(property.Id, ownerId, false, title: "Titlu nou", description: "Descriere noua", price: 600m),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Draft", result!.Status);
    }

    [Fact]
    public async Task Handle_UpdatesLocationDetails()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        var location = Imova.Domain.Locations.PropertyLocation.Create(property.Id, "Moldova", "Chisinau", null, 47.0105, 28.8638);
        dbContext.PropertyLocations.Add(location);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var geocodingService = new FakeGeocodingService { ResultToReturn = new(47.75, 27.9167, "Balti, Moldova") };
        var handler = new UpdatePropertyHandler(dbContext, geocodingService);
        var result = await handler.Handle(
            BuildCommand(property.Id, ownerId, false, city: "Balti", district: "Centru"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result!.Location);
        Assert.Equal("Balti", result.Location!.City);
        Assert.Equal("Centru", result.Location.District);
        Assert.Equal(47.75, result.Location.Latitude);
        Assert.Equal(27.9167, result.Location.Longitude);
        Assert.Equal("Centru, Balti, Moldova", geocodingService.LastAddressRequested);
    }

    [Fact]
    public async Task Handle_WhenGeocodingFails_KeepsListingSavedWithNullCoordinates()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        var location = Imova.Domain.Locations.PropertyLocation.Create(property.Id, "Moldova", "Chisinau", null, 47.0105, 28.8638);
        dbContext.PropertyLocations.Add(location);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var geocodingService = new FakeGeocodingService { ResultToReturn = null };
        var handler = new UpdatePropertyHandler(dbContext, geocodingService);
        var result = await handler.Handle(
            BuildCommand(property.Id, ownerId, false, city: "Nonexistent Place"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result!.Location);
        Assert.Null(result.Location!.Latitude);
        Assert.Null(result.Location.Longitude);
    }

    [Fact]
    public async Task Handle_UpdatesPropertyTypeAndListingType()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdatePropertyHandler(dbContext, new FakeGeocodingService());
        var result = await handler.Handle(
            BuildCommand(
                property.Id, ownerId, false,
                propertyType: PropertyType.House, listingType: ListingType.Sale,
                area: 120m, rooms: 4m, floor: null, totalFloors: null),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("House", result!.PropertyType);
        Assert.Equal("Sale", result.ListingType);
    }

    [Fact]
    public async Task Handle_WithStreetAddress_IncludesItInTheGeocodedAddressAndPersistsIt()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        var location = Imova.Domain.Locations.PropertyLocation.Create(property.Id, "Moldova", "Chisinau", null, null, null);
        dbContext.PropertyLocations.Add(location);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var geocodingService = new FakeGeocodingService
        {
            ResultToReturn = new(47.0105, 28.8638, "Str. Ismail 44, Botanica, Chisinau, Moldova"),
        };
        var handler = new UpdatePropertyHandler(dbContext, geocodingService);
        var result = await handler.Handle(
            BuildCommand(property.Id, ownerId, false, district: "Botanica", streetAddress: "Str. Ismail 44"),
            CancellationToken.None);

        Assert.Equal("Str. Ismail 44, Botanica, Chisinau, Moldova", geocodingService.LastAddressRequested);
        Assert.Equal("Str. Ismail 44", result!.Location!.Street);
    }
}
