using Imova.Contracts.Listings;
using Imova.Application.Features.Listings;
using Imova.Domain.Amenities;
using Imova.Domain.Listings;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;
using Imova.Domain.Proximities;
using Imova.Domain.Publishers;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class ListingMappingTests
{
    private static readonly Publisher Publisher = Publisher.CreateIndividual(Guid.NewGuid(), "Ion", "+373 69 123 456", "ion@example.com");

    private static PropertyLocation Location(double? lat = 47.0105, double? lon = 28.8638) =>
        PropertyLocation.Create("Moldova", Guid.NewGuid(), "Chișinău", null, null, Guid.NewGuid(), "Botanica", lat, lon, "Strada Ismail", "44");

    [Fact]
    public void ToDto_MapsPropertyListingAndPublisher()
    {
        var parking = new Amenity(Guid.NewGuid(), "parking", "Parcare");
        var school = new Proximity(Guid.NewGuid(), "school", "Școală");
        var location = Location();
        var property = Property.Create(
            PropertyType.Room, 18m, 1975, PropertyCondition.NeedsRepair, location.Id,
            new RoomAttributes(BathroomType.Shared, 2), [parking.Id], [school.Id]);
        var listing = ListingTestData.NewListing(property.Id, Publisher.Id);

        var dto = listing.ToDto(
            property, location, Publisher, new Dictionary<Guid, Amenity> { [parking.Id] = parking },
            new Dictionary<Guid, Proximity> { [school.Id] = school }, [], isSaved: true,
            includeContactDetails: true);

        Assert.Equal("Room", dto.Property.PropertyType);
        Assert.Equal("NeedsRepair", dto.Property.Condition);
        Assert.Equal("Shared", dto.Property.TypeSpecificAttributes.GetProperty("bathroomType").GetString());
        Assert.Equal("parking", Assert.Single(dto.Property.Amenities).Key);
        Assert.Equal("school", Assert.Single(dto.Property.Proximities).Key);
        Assert.Equal("Botanica", dto.Property.Location!.ChisinauSectorName);
        Assert.Equal("44", dto.Property.Location.BuildingNumber);
        Assert.Equal("Rent", dto.TransactionType);
        Assert.Equal("Draft", dto.Status);
        Assert.Equal("EUR", dto.Price.Currency);
        Assert.Equal("Individual", dto.Publisher.PublisherType);
        // The publisher's own phone/email never ride along — the listing's Contact is what's shown.
        Assert.Null(dto.Publisher.Email);
        Assert.Null(dto.Publisher.Phone);
        Assert.True(dto.IsSaved);
    }

    [Fact]
    public void ToDto_WithoutContactDetails_BlanksPhoneAndEmailOnly()
    {
        var location = Location();
        var property = Property.Create(PropertyType.Garage, 18m, null, null, location.Id, new GarageAttributes(ParkingType.Garage));
        var listing = ListingTestData.NewListing(property.Id, Publisher.Id);

        var dto = listing.ToDto(
            property, location, Publisher, new Dictionary<Guid, Amenity>(), new Dictionary<Guid, Proximity>(), [], false,
            includeContactDetails: false);

        Assert.Null(dto.Publisher.Phone);
        Assert.Null(dto.Publisher.Email);
        Assert.Equal("Ion", dto.Publisher.DisplayName);
    }

    [Fact]
    public void ToDto_WithNullCoordinatesOrNoLocation_MapsThemAsNull()
    {
        var location = Location(null, null);
        var property = Property.Create(PropertyType.Garage, 18m, null, null, location.Id, new GarageAttributes(ParkingType.Garage));
        var listing = ListingTestData.NewListing(property.Id, Publisher.Id);
        var amenities = new Dictionary<Guid, Amenity>();
        var proximities = new Dictionary<Guid, Proximity>();

        Assert.Null(listing.ToDto(property, location, Publisher, amenities, proximities, [], false, false).Property.Location!.Latitude);
        Assert.Null(listing.ToDto(property, null, Publisher, amenities, proximities, [], false, false).Property.Location);
    }

    // --- Contact ---

    private static ListingDto DetailFor(ListingContact? contact, bool canSeeHiddenPhone = false, bool includeContactDetails = true)
    {
        var location = Location();
        var property = Property.Create(PropertyType.Garage, 18m, null, null, location.Id, new GarageAttributes(ParkingType.Garage));
        var listing = ListingTestData.NewListing(property.Id, Publisher.Id, contact: contact);
        return listing.ToDto(
            property, location, Publisher, new Dictionary<Guid, Amenity>(), new Dictionary<Guid, Proximity>(), [], false,
            includeContactDetails, canSeeHiddenPhone);
    }

    [Fact]
    public void Contact_Self_TakesNameAndEmailFromThePublisherButKeepsItsOwnPhone()
    {
        var contact = DetailFor(TestContacts.Self with { MessagingApps = [ContactMessagingApp.Viber] }).Contact!;

        Assert.Equal("Self", contact.PersonType);
        Assert.Equal("Ion", contact.Name);
        Assert.Equal("ion@example.com", contact.Email);
        Assert.Equal("+373 69 111 222", contact.Phone);
        Assert.Equal(["Viber"], contact.MessagingApps);
        Assert.Equal("Any", contact.PreferredContactMethod);
    }

    [Fact]
    public void Contact_Other_UsesItsOwnNamePhoneAndEmail()
    {
        var contact = DetailFor(TestContacts.Other).Contact!;

        Assert.Equal("Other", contact.PersonType);
        Assert.Equal("Maria Popescu", contact.Name);
        Assert.Equal("maria@example.com", contact.Email);
        Assert.Equal("+373 79 333 444", contact.Phone);
    }

    [Fact]
    public void Contact_HiddenPhone_IsLeftOutForThePublic()
    {
        var dto = DetailFor(TestContacts.HiddenPhone);

        Assert.True(dto.Contact!.HidePhoneNumber);
        Assert.Null(dto.Contact.Phone);
        Assert.Null(dto.Publisher.Phone);
        Assert.Equal("PlatformMessages", dto.Contact.PreferredContactMethod);
        // Name/email stay — only the number is hidden.
        Assert.Equal("Ion", dto.Contact.Name);
    }

    [Fact]
    public void Contact_HiddenPhone_IsStillShownToTheOwnerOrAnAdmin()
    {
        Assert.Equal("+373 69 555 666", DetailFor(TestContacts.HiddenPhone, canSeeHiddenPhone: true).Contact!.Phone);
    }

    [Fact]
    public void Contact_IsLeftOffCardsAndSearchResults()
    {
        Assert.Null(DetailFor(TestContacts.Self, includeContactDetails: false).Contact);
    }

    [Fact]
    public void Contact_OfAListingFromBeforeTheContactStep_FallsBackToThePublisher()
    {
        var contact = DetailFor(null).Contact!;

        Assert.Equal("Self", contact.PersonType);
        Assert.Equal("+373 69 123 456", contact.Phone);
        Assert.Equal("ion@example.com", contact.Email);
        Assert.False(contact.HidePhoneNumber);
        Assert.Equal("Any", contact.PreferredContactMethod);
    }
}
