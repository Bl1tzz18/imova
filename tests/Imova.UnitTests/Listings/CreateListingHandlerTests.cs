using System.Text.Json;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Listings.CreateListing;
using Imova.Domain.Listings;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;
using Imova.Domain.Publishers;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Listings;

public class CreateListingHandlerTests
{
    private readonly ImovaDbContext _dbContext = TestDbContextFactory.Create();
    private readonly FakeGeocodingService _geocoding = new();
    private readonly Raion _raion;
    private readonly Localitate _localitate;
    private readonly ChisinauSector _sector;
    private readonly ApplicationUser _user;

    public CreateListingHandlerTests()
    {
        _raion = Raion.Create(Guid.NewGuid(), "0100", "Chisinau", null, LocalityLabel.Sector);
        _localitate = Localitate.Create(Guid.NewGuid(), _raion.Id, null, "0101", "Durlești", null);
        _sector = ChisinauSector.Create("Botanica");
        _user = new ApplicationUser
        {
            Id = Guid.NewGuid(), Email = "ion@example.com", UserName = "ion@example.com", DisplayName = "Ion", PhoneNumber = "+373 69 123 456",
        };
        _dbContext.Raioane.Add(_raion);
        _dbContext.Localitati.Add(_localitate);
        _dbContext.ChisinauSectors.Add(_sector);
        _dbContext.Users.Add(_user);
        _dbContext.SaveChanges();
    }

    private CreateListingHandler Handler() =>
        new(_dbContext, _geocoding, new FakeExchangeRateProvider(), new FakeBlobStorageService());

    private CreateListingCommand Command(
        Guid? id = null,
        Guid? publisherId = null,
        TransactionType transactionType = TransactionType.Rent,
        decimal price = 550m,
        Currency currency = Currency.EUR,
        RentalDetails? rentalDetails = null,
        IReadOnlyList<Guid>? amenityIds = null,
        Guid? localitateId = null,
        Guid? chisinauSectorId = null,
        string? streetAddress = null,
        string? buildingNumber = null) =>
        new(
            id,
            _user.Id,
            publisherId,
            PropertyType.Apartment,
            54m,
            1985,
            PropertyCondition.Renovated,
            JsonDocument.Parse("""{"rooms":2,"floor":3,"totalFloors":9,"heatingSystem":"DistrictHeating"}""").RootElement.Clone(),
            amenityIds,
            "Moldova",
            _raion.Id,
            localitateId,
            chisinauSectorId,
            streetAddress,
            buildingNumber,
            transactionType,
            "Apartament 2 camere",
            "Apartament luminos, aproape de centru.",
            price,
            currency,
            true,
            rentalDetails);

    [Fact]
    public async Task Handle_CreatesPropertyLocationAndListingTogether()
    {
        var result = await Handler().Handle(Command(localitateId: _localitate.Id), CancellationToken.None);

        var listing = Assert.Single(_dbContext.Listings);
        var property = Assert.Single(_dbContext.Properties);
        var location = Assert.Single(_dbContext.PropertyLocations);
        Assert.Equal(property.Id, listing.PropertyId);
        Assert.Equal(location.Id, property.LocationId);
        Assert.Equal(new ApartmentAttributes(Rooms: 2, Floor: 3, TotalFloors: 9, HeatingSystem: HeatingSystem.DistrictHeating), property.TypeSpecificAttributes);
        Assert.Equal(1985, property.YearBuilt);
        Assert.Equal(PropertyCondition.Renovated, property.Condition);

        Assert.Equal(listing.Id, result.Id);
        Assert.Equal("Apartment", result.Property.PropertyType);
        Assert.Equal(2, result.Property.TypeSpecificAttributes.GetProperty("rooms").GetInt32());
        Assert.Equal("Durlești", result.Property.Location!.LocalitateName);
    }

    [Fact]
    public async Task Handle_SubmitsTheNewListingStraightForReview()
    {
        var result = await Handler().Handle(Command(), CancellationToken.None);

        Assert.Equal("PendingReview", result.Status);
    }

    [Fact]
    public async Task Handle_WithoutPublisherId_PublishesUnderTheUsersIndividualPublisher_CreatingItIfMissing()
    {
        var result = await Handler().Handle(Command(), CancellationToken.None);

        var publisher = Assert.Single(_dbContext.Publishers);
        Assert.Equal(_user.Id, publisher.UserId);
        Assert.Equal(PublisherType.Individual, publisher.PublisherType);
        Assert.Equal(publisher.Id, result.Publisher.Id);
        Assert.Equal("Ion", result.Publisher.DisplayName);
    }

    [Fact]
    public async Task Handle_WithoutPublisherId_ReusesAnExistingIndividualPublisher()
    {
        var existing = ListingTestData.AddIndividualPublisher(_dbContext, _user.Id);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await Handler().Handle(Command(), CancellationToken.None);

        Assert.Equal(existing.Id, result.Publisher.Id);
        Assert.Single(_dbContext.Publishers);
    }

    [Fact]
    public async Task Handle_WithTheUsersAgencyPublisherId_PublishesUnderTheAgency()
    {
        ListingTestData.AddIndividualPublisher(_dbContext, _user.Id);
        var agency = ListingTestData.AddAgencyPublisher(_dbContext, _user.Id);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await Handler().Handle(Command(publisherId: agency.Id), CancellationToken.None);

        Assert.Equal(agency.Id, Assert.Single(_dbContext.Listings).PublisherId);
        Assert.Equal("Agency", result.Publisher.PublisherType);
    }

    [Fact]
    public async Task Handle_WithSomeoneElsesPublisherId_ThrowsForbiddenAndCreatesNothing()
    {
        var foreign = ListingTestData.AddAgencyPublisher(_dbContext, Guid.NewGuid());
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            Handler().Handle(Command(publisherId: foreign.Id), CancellationToken.None));

        Assert.Empty(_dbContext.Listings);
        Assert.Empty(_dbContext.Properties);
    }

    [Fact]
    public async Task Handle_WithUnknownPublisherId_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            Handler().Handle(Command(publisherId: Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ComputesPriceEurFromTheChosenCurrency()
    {
        var result = await Handler().Handle(Command(price: 20_000m, currency: Currency.MDL), CancellationToken.None);

        Assert.Equal(20_000m, result.Price.Amount);
        Assert.Equal("MDL", result.Price.Currency);
        Assert.Equal(1_000m, result.Price.PriceEur);
        Assert.True(result.Price.IsNegotiable);
    }

    [Fact]
    public async Task Handle_RentalWithoutRentalDetails_GetsDefaultRentalTerms()
    {
        var result = await Handler().Handle(Command(rentalDetails: null), CancellationToken.None);

        Assert.NotNull(result.RentalDetails);
        Assert.Equal("Unfurnished", result.RentalDetails!.FurnishedStatus);
        Assert.Null(result.SaleDetails);
    }

    [Fact]
    public async Task Handle_RentalWithRentalDetails_PersistsThem()
    {
        var terms = new RentalDetails(6, 500m, true, FurnishedStatus.Furnished, new DateTime(2026, 11, 1), true);

        await Handler().Handle(Command(rentalDetails: terms), CancellationToken.None);

        Assert.Equal(terms, Assert.Single(_dbContext.Listings).RentalDetails);
    }

    [Fact]
    public async Task Handle_Sale_GetsSaleDetailsAndNoRentalDetails()
    {
        var result = await Handler().Handle(Command(transactionType: TransactionType.Sale), CancellationToken.None);

        Assert.Equal("Sale", result.TransactionType);
        Assert.NotNull(result.SaleDetails);
        Assert.Null(result.RentalDetails);
    }

    [Fact]
    public async Task Handle_PersistsAmenitiesOnTheProperty()
    {
        var parking = Guid.NewGuid();
        var balcony = Guid.NewGuid();
        _dbContext.Amenities.AddRange(
            new Imova.Domain.Amenities.Amenity(parking, "parking", "Parcare"),
            new Imova.Domain.Amenities.Amenity(balcony, "balcony", "Balcon/Logie"));
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await Handler().Handle(Command(amenityIds: [parking, balcony]), CancellationToken.None);

        var property = await _dbContext.Properties.Include(p => p.Amenities).SingleAsync();
        Assert.Equal(new[] { balcony, parking }.Order(), property.Amenities.Select(a => a.AmenityId).Order());
        Assert.Equal(["balcony", "parking"], result.Property.Amenities.Select(a => a.Key).Order());
    }

    [Fact]
    public async Task Handle_WithClientSuppliedId_UsesItForTheListing()
    {
        var id = Guid.NewGuid();

        var result = await Handler().Handle(Command(id: id), CancellationToken.None);

        Assert.Equal(id, result.Id);
        Assert.Equal(id, Assert.Single(_dbContext.Listings).Id);
    }

    [Fact]
    public async Task Handle_WhenGeocodingResolves_StoresCoordinates()
    {
        _geocoding.ResultToReturn = new(47.1, 28.9, "x");

        var result = await Handler().Handle(Command(), CancellationToken.None);

        Assert.Equal(47.1, result.Property.Location!.Latitude);
        Assert.Equal(28.9, result.Property.Location.Longitude);
    }

    [Fact]
    public async Task Handle_WhenGeocodingFails_StillCreatesTheListingWithoutCoordinates()
    {
        _geocoding.ResultToReturn = null;

        var result = await Handler().Handle(Command(), CancellationToken.None);

        Assert.Single(_dbContext.Listings);
        Assert.Null(result.Property.Location!.Latitude);
    }

    [Fact]
    public async Task Handle_GeocodesStreetWithBuildingNumberAndSector_AndPersistsThemSeparately()
    {
        var result = await Handler().Handle(
            Command(chisinauSectorId: _sector.Id, streetAddress: "Strada Ismail", buildingNumber: "44A"), CancellationToken.None);

        Assert.Equal("Strada Ismail 44A, Botanica, Chisinau, Moldova", _geocoding.LastAddressRequested);
        Assert.Equal("Strada Ismail", result.Property.Location!.Street);
        Assert.Equal("44A", result.Property.Location.BuildingNumber);
        Assert.Equal("Botanica", result.Property.Location.ChisinauSectorName);
    }

    [Fact]
    public async Task Handle_ReturnsThePublishersContactDetailsToTheCreator()
    {
        var result = await Handler().Handle(Command(), CancellationToken.None);

        Assert.Equal("ion@example.com", result.Publisher.Email);
        Assert.Equal("+373 69 123 456", result.Publisher.Phone);
    }
}
