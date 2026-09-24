using Imova.Application.Features.Listings;
using Imova.Domain.Amenities;
using Imova.Domain.Listings;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;
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
        var location = Location();
        var property = Property.Create(
            PropertyType.Room, 18m, 1975, PropertyCondition.NeedsRepair, location.Id,
            new RoomAttributes(BathroomType.Shared, 2), [parking.Id]);
        var listing = ListingTestData.NewListing(property.Id, Publisher.Id);

        var dto = listing.ToDto(
            property, location, Publisher, new Dictionary<Guid, Amenity> { [parking.Id] = parking }, [], isSaved: true,
            includeContactDetails: true);

        Assert.Equal("Room", dto.Property.PropertyType);
        Assert.Equal("NeedsRepair", dto.Property.Condition);
        Assert.Equal("Shared", dto.Property.TypeSpecificAttributes.GetProperty("privateOrSharedBathroom").GetString());
        Assert.Equal("parking", Assert.Single(dto.Property.Amenities).Key);
        Assert.Equal("Botanica", dto.Property.Location!.ChisinauSectorName);
        Assert.Equal("44", dto.Property.Location.BuildingNumber);
        Assert.Equal("Rent", dto.TransactionType);
        Assert.Equal("Draft", dto.Status);
        Assert.Equal("EUR", dto.Price.Currency);
        Assert.Equal("Individual", dto.Publisher.PublisherType);
        Assert.Equal("ion@example.com", dto.Publisher.Email);
        Assert.True(dto.IsSaved);
    }

    [Fact]
    public void ToDto_WithoutContactDetails_BlanksPhoneAndEmailOnly()
    {
        var location = Location();
        var property = Property.Create(PropertyType.Garage, 18m, null, null, location.Id, new GarageAttributes(GarageType.Box));
        var listing = ListingTestData.NewListing(property.Id, Publisher.Id);

        var dto = listing.ToDto(property, location, Publisher, new Dictionary<Guid, Amenity>(), [], false, includeContactDetails: false);

        Assert.Null(dto.Publisher.Phone);
        Assert.Null(dto.Publisher.Email);
        Assert.Equal("Ion", dto.Publisher.DisplayName);
    }

    [Fact]
    public void ToDto_WithNullCoordinatesOrNoLocation_MapsThemAsNull()
    {
        var location = Location(null, null);
        var property = Property.Create(PropertyType.Garage, 18m, null, null, location.Id, new GarageAttributes(GarageType.Box));
        var listing = ListingTestData.NewListing(property.Id, Publisher.Id);
        var amenities = new Dictionary<Guid, Amenity>();

        Assert.Null(listing.ToDto(property, location, Publisher, amenities, [], false, false).Property.Location!.Latitude);
        Assert.Null(listing.ToDto(property, null, Publisher, amenities, [], false, false).Property.Location);
    }
}
