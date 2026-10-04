using System.Text.Json;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Listings.UpdateListing;
using Imova.Domain.Amenities;
using Imova.Domain.Proximities;
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
    private readonly Guid _school = Guid.NewGuid();
    private readonly Guid _park = Guid.NewGuid();

    public UpdateListingHandlerTests()
    {
        _raion = Raion.Create(Guid.NewGuid(), "0100", "Chisinau", null, LocalityLabel.Sector);
        _sector = ChisinauSector.Create("Centru");
        _dbContext.Raioane.Add(_raion);
        _dbContext.ChisinauSectors.Add(_sector);
        _dbContext.Amenities.AddRange(new Amenity(_parking, "parking", "Parcare"), new Amenity(_balcony, "balcony", "Balcon/Logie"));
        _dbContext.Proximities.AddRange(new Proximity(_school, "school", "Școală"), new Proximity(_park, "park", "Parc / zonă verde"));
        ListingTestData.AddUser(_dbContext, _ownerId);
        var publisher = ListingTestData.AddIndividualPublisher(_dbContext, _ownerId);
        var property = ListingTestData.AddProperty(_dbContext, amenityIds: [_parking], proximityIds: [_school]);
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
        Guid? agencyId = null,
        PropertyType propertyType = PropertyType.Apartment,
        string attributesJson = """{"rooms":3,"floor":4,"totalFloors":10}""",
        IReadOnlyList<Guid>? amenityIds = null,
        IReadOnlyList<Guid>? proximityIds = null,
        TransactionType transactionType = TransactionType.Rent,
        RentalDetails? rentalDetails = null,
        Guid? chisinauSectorId = null,
        string? streetAddress = "Strada Ismail",
        string? buildingNumber = null,
        ListingContact? contact = null,
        decimal price = 600m,
        Currency currency = Currency.USD,
        bool isNegotiable = false) =>
        new(
            id ?? _listing.Id,
            requestingUserId ?? _ownerId,
            isAdmin,
            agencyId,
            propertyType,
            72m,
            2010,
            PropertyCondition.New,
            JsonDocument.Parse(attributesJson).RootElement.Clone(),
            amenityIds,
            proximityIds,
            "Moldova",
            _raion.Id,
            null,
            chisinauSectorId,
            streetAddress,
            buildingNumber,
            transactionType,
            "Titlu nou",
            "Descriere nouă",
            price,
            currency,
            isNegotiable,
            rentalDetails,
            contact ?? TestContacts.Self);

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
    public async Task Handle_WhenThePriceChanges_RecordsTheOldAndNewPrice()
    {
        // The listing starts at 550 EUR; the edit asks 600 USD (540 EUR at the fake rate).
        await Handler().Handle(Command(), CancellationToken.None);

        var change = await _dbContext.ListingPriceChanges.SingleAsync();
        Assert.Equal(_listing.Id, change.ListingId);
        Assert.Equal((550m, Currency.EUR, 550m), (change.OldAmount, change.OldCurrency, change.OldPriceEur));
        Assert.Equal((600m, Currency.USD, 540m), (change.NewAmount, change.NewCurrency, change.NewPriceEur));
        Assert.Equal(_listing.UpdatedAt, change.ChangedAt);
    }

    [Fact]
    public async Task Handle_WhenOnlyOtherDetailsOrNegotiableChange_RecordsNoPriceChange()
    {
        await Handler().Handle(Command(price: 550m, currency: Currency.EUR, isNegotiable: true), CancellationToken.None);

        Assert.Empty(_dbContext.ListingPriceChanges);
    }

    [Fact]
    public async Task Handle_EachPriceEdit_AddsToTheHistory()
    {
        await Handler().Handle(Command(price: 500m, currency: Currency.EUR), CancellationToken.None);
        await Handler().Handle(Command(price: 480m, currency: Currency.EUR), CancellationToken.None);

        var history = await _dbContext.ListingPriceChanges.OrderBy(c => c.ChangedAt).ThenBy(c => c.OldAmount)
            .Select(c => new { c.OldAmount, c.NewAmount }).ToListAsync();
        Assert.Equal(2, history.Count);
        Assert.Contains(history, c => c.OldAmount == 550m && c.NewAmount == 500m);
        Assert.Contains(history, c => c.OldAmount == 500m && c.NewAmount == 480m);
    }

    [Fact]
    public async Task Handle_ByTheAuthorOfAnAgencyListing_Succeeds()
    {
        var agencyOwner = Guid.NewGuid();
        var agency = ListingTestData.AddAgency(_dbContext, agencyOwner);
        var author = ListingTestData.AddIndividualPublisher(_dbContext, agencyOwner);
        var listing = ListingTestData.AddListing(_dbContext, author.Id, agencyId: agency.Id);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await Handler().Handle(Command(requestingUserId: agencyOwner, id: listing.Id, agencyId: agency.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(agency.Id, listing.AgencyId);
    }

    // An agency listing written by an Agent, with the agency's Owner, an Admin and another Agent.
    private async Task<(Listing Listing, Guid AgencyId, Guid Owner, Guid Admin, Guid Agent, Guid OtherAgent)> AgencyListingAsync()
    {
        var owner = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var agent = Guid.NewGuid();
        var otherAgent = Guid.NewGuid();
        var agency = ListingTestData.AddAgency(_dbContext, owner);
        agency.AddMember(admin, Imova.Domain.Agencies.AgencyRole.Admin, DateTimeOffset.UtcNow);
        agency.AddMember(agent, Imova.Domain.Agencies.AgencyRole.Agent, DateTimeOffset.UtcNow);
        agency.AddMember(otherAgent, Imova.Domain.Agencies.AgencyRole.Agent, DateTimeOffset.UtcNow);
        var listing = ListingTestData.AddListing(_dbContext, ListingTestData.AddIndividualPublisher(_dbContext, agent).Id, agencyId: agency.Id);
        await _dbContext.SaveChangesAsync(CancellationToken.None);
        return (listing, agency.Id, owner, admin, agent, otherAgent);
    }

    [Fact]
    public async Task AnAgencysAdmin_CanEditAnAgentsListing_ButAnotherAgentCant()
    {
        var (listing, agencyId, _, admin, _, otherAgent) = await AgencyListingAsync();

        Assert.NotNull(await Handler().Handle(Command(requestingUserId: admin, id: listing.Id, agencyId: agencyId), CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            Handler().Handle(Command(requestingUserId: otherAgent, id: listing.Id, agencyId: agencyId), CancellationToken.None));
    }

    [Fact]
    public async Task OnlyTheAuthor_MovesAListingOutOfItsAgency()
    {
        var (listing, agencyId, owner, _, agent, _) = await AgencyListingAsync();

        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            Handler().Handle(Command(requestingUserId: owner, id: listing.Id, agencyId: null), CancellationToken.None));
        Assert.Equal(ErrorCodes.ListingAgencyChangeAuthorOnly, ex.Code);
        Assert.Equal(agencyId, listing.AgencyId);

        await Handler().Handle(Command(requestingUserId: agent, id: listing.Id, agencyId: null), CancellationToken.None);
        Assert.Null(listing.AgencyId);
    }

    [Fact]
    public async Task TheAuthor_MovesAPrivateListingIntoTheirAgency_ButNotIntoAnotherOne()
    {
        var mine = ListingTestData.AddAgency(_dbContext, _ownerId, "A mea");
        var theirs = ListingTestData.AddAgency(_dbContext, Guid.NewGuid(), "A lor");
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            Handler().Handle(Command(agencyId: theirs.Id), CancellationToken.None));
        Assert.Equal(ErrorCodes.ListingNotAgencyMember, ex.Code);

        var result = await Handler().Handle(Command(agencyId: mine.Id), CancellationToken.None);
        Assert.Equal(mine.Id, _listing.AgencyId);
        Assert.Equal("A mea", result!.Agency!.Name);
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
    public async Task Handle_ForRejectedListing_OfAnOwnerWithAnUnconfirmedEmail_KeepsItRejected()
    {
        (await _dbContext.Users.SingleAsync(u => u.Id == _ownerId)).EmailConfirmed = false;
        _listing.MoveTo(ListingStatus.Rejected);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await Handler().Handle(Command(), CancellationToken.None);

        Assert.Equal("Rejected", result!.Status);
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
    public async Task Handle_ReplacesTheContact()
    {
        var result = await Handler().Handle(Command(contact: TestContacts.Other), CancellationToken.None);

        var stored = (await _dbContext.Listings.SingleAsync()).Contact!;
        Assert.Equal(ContactPersonType.Other, stored.PersonType);
        Assert.Equal("Maria Popescu", result!.Contact!.Name);
        Assert.Equal("+373 79 333 444", result.Contact.Phone);
    }

    [Fact]
    public async Task Handle_ReplacesProximities()
    {
        var result = await Handler().Handle(Command(proximityIds: [_park]), CancellationToken.None);

        var property = await _dbContext.Properties.Include(p => p.Proximities).SingleAsync();
        Assert.Equal(_park, Assert.Single(property.Proximities).ProximityId);
        Assert.Equal("park", Assert.Single(result!.Property.Proximities).Key);
    }

    [Fact]
    public async Task Handle_WithoutProximities_ClearsThem()
    {
        await Handler().Handle(Command(), CancellationToken.None);

        var property = await _dbContext.Properties.Include(p => p.Proximities).SingleAsync();
        Assert.Empty(property.Proximities);
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
