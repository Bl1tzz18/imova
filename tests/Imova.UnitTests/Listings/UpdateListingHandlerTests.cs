using System.Text.Json;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Listings.UpdateListing;
using Imova.Domain.Amenities;
using Imova.Domain.Listings;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Listings;

public class UpdateListingHandlerTests
{
    private readonly ImovaDbContext _dbContext = TestDbContextFactory.Create();
    private readonly FakeGeocodingService _geocoding = new();
    private readonly Raion _raion;
    private readonly ChisinauSector _sector;
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Listing _listing;
    private readonly Guid _parking = Guid.NewGuid();
    private readonly Guid _balcony = Guid.NewGuid();

    public UpdateListingHandlerTests()
    {
        _raion = Raion.Create(Guid.NewGuid(), "0100", "Chisinau", null, LocalityLabel.Sector);
        _sector = ChisinauSector.Create("Centru");
        _dbContext.Raioane.Add(_raion);
        _dbContext.ChisinauSectors.Add(_sector);
        _dbContext.Amenities.AddRange(new Amenity(_parking, "parking", "Parcare"), new Amenity(_balcony, "balcony", "Balcon/Logie"));
        var publisher = ListingTestData.AddIndividualPublisher(_dbContext, _ownerId);
        var property = ListingTestData.AddProperty(_dbContext, amenityIds: [_parking]);
        _listing = ListingTestData.NewListing(property.Id, publisher.Id);
        _dbContext.Listings.Add(_listing);
        _dbContext.SaveChanges();
    }

    private UpdateListingHandler Handler() =>
        new(_dbContext, _geocoding, new FakeExchangeRateProvider(), new FakeBlobStorageService());

    private UpdateListingCommand Command(
        Guid? requestingUserId = null,
        bool isAdmin = false,
        Guid? id = null,
        PropertyType propertyType = PropertyType.Apartment,
        string attributesJson = """{"rooms":3,"floor":4,"totalFloors":10}""",
        IReadOnlyList<Guid>? amenityIds = null,
        TransactionType transactionType = TransactionType.Rent,
        RentalDetails? rentalDetails = null,
        Guid? chisinauSectorId = null,
        string? streetAddress = "Strada Ismail",
        string? buildingNumber = null) =>
        new(
            id ?? _listing.Id,
            requestingUserId ?? _ownerId,
            isAdmin,
            propertyType,
            72m,
            2010,
            PropertyCondition.New,
            JsonDocument.Parse(attributesJson).RootElement.Clone(),
            amenityIds,
            "Moldova",
            _raion.Id,
            null,
            chisinauSectorId,
            streetAddress,
            buildingNumber,
            transactionType,
            "Titlu nou",
            "Descriere nouă",
            600m,
            Currency.USD,
            false,
            rentalDetails);

    [Fact]
    public async Task Handle_ByOwner_UpdatesBothThePropertyAndTheListing()
    {
        var result = await Handler().Handle(Command(), CancellationToken.None);

        var property = await _dbContext.Properties.SingleAsync();
        Assert.Equal(72m, property.TotalAreaM2);
        Assert.Equal(new ApartmentAttributes(Rooms: 3, Floor: 4, TotalFloors: 10), property.TypeSpecificAttributes);
        Assert.Equal(PropertyCondition.New, property.Condition);
        Assert.Equal("Titlu nou", result!.Title);
        Assert.Equal(600m, result.Price.Amount);
        Assert.Equal(540m, result.Price.PriceEur);
    }

    [Fact]
    public async Task Handle_ByOwnerViaAgencyPublisher_Succeeds()
    {
        var agencyOwner = Guid.NewGuid();
        var agency = ListingTestData.AddAgencyPublisher(_dbContext, agencyOwner);
        var listing = ListingTestData.AddListing(_dbContext, agency.Id);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await Handler().Handle(Command(requestingUserId: agencyOwner, id: listing.Id), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_ByNonOwnerNonAdmin_ThrowsForbiddenAccessException()
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            Handler().Handle(Command(requestingUserId: Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("Apartament 2 camere", (await _dbContext.Listings.SingleAsync()).Title);
    }

    [Fact]
    public async Task Handle_ByAdminNonOwner_Succeeds()
    {
        var result = await Handler().Handle(Command(requestingUserId: Guid.NewGuid(), isAdmin: true), CancellationToken.None);

        Assert.Equal("Titlu nou", result!.Title);
    }

    [Fact]
    public async Task Handle_ForUnknownListingId_ReturnsNull()
    {
        Assert.Null(await Handler().Handle(Command(id: Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForRejectedListing_ResubmitsForReview()
    {
        _listing.MoveTo(ListingStatus.Rejected);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await Handler().Handle(Command(), CancellationToken.None);

        Assert.Equal("PendingReview", result!.Status);
        Assert.Null(result.RejectionReason);
    }

    [Fact]
    public async Task Handle_ForActiveListing_KeepsItActive()
    {
        _listing.MoveTo(ListingStatus.Active);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await Handler().Handle(Command(), CancellationToken.None);

        Assert.Equal("Active", result!.Status);
    }

    [Fact]
    public async Task Handle_ReplacesAmenities()
    {
        await Handler().Handle(Command(amenityIds: [_balcony]), CancellationToken.None);

        var property = await _dbContext.Properties.Include(p => p.Amenities).SingleAsync();
        Assert.Equal(_balcony, Assert.Single(property.Amenities).AmenityId);
    }

    [Fact]
    public async Task Handle_ChangingPropertyType_StoresTheNewTypesAttributes()
    {
        await Handler().Handle(
            Command(propertyType: PropertyType.House, attributesJson: """{"rooms":5,"houseFloors":2}"""), CancellationToken.None);

        var property = await _dbContext.Properties.SingleAsync();
        Assert.Equal(PropertyType.House, property.PropertyType);
        Assert.Equal(new HouseAttributes(Rooms: 5, HouseFloors: 2), property.TypeSpecificAttributes);
    }

    [Fact]
    public async Task Handle_SwitchingRentToSale_DropsRentalDetails()
    {
        var result = await Handler().Handle(Command(transactionType: TransactionType.Sale), CancellationToken.None);

        Assert.Equal("Sale", result!.TransactionType);
        Assert.Null(result.RentalDetails);
        Assert.NotNull(result.SaleDetails);
    }

    [Fact]
    public async Task Handle_UpdatesLocationAndRegeocodesTheFullAddress()
    {
        _geocoding.ResultToReturn = new(47.02, 28.83, "x");

        var result = await Handler().Handle(
            Command(chisinauSectorId: _sector.Id, streetAddress: "Strada Ismail", buildingNumber: "12"), CancellationToken.None);

        Assert.Equal("Strada Ismail 12, Centru, Chisinau, Moldova", _geocoding.LastAddressRequested);
        var location = await _dbContext.PropertyLocations.SingleAsync();
        Assert.Equal("Centru", location.ChisinauSectorName);
        Assert.Equal("12", location.BuildingNumber);
        Assert.Equal(47.02, result!.Property.Location!.Latitude);
    }

    [Fact]
    public async Task Handle_WhenGeocodingFails_StillSavesWithNullCoordinates()
    {
        _geocoding.ResultToReturn = null;

        await Handler().Handle(Command(), CancellationToken.None);

        Assert.Null((await _dbContext.PropertyLocations.SingleAsync()).Latitude);
        Assert.Equal("Titlu nou", (await _dbContext.Listings.SingleAsync()).Title);
    }
}
