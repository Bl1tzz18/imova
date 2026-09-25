using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;

namespace Imova.UnitTests.Properties;

public class PropertyTests
{
    private static readonly Guid LocationId = Guid.NewGuid();
    private static readonly ApartmentAttributes Apartment = new(Rooms: 2, Floor: 3, TotalFloors: 9);

    [Fact]
    public void Create_WithValidData_SetsPhysicalFields()
    {
        var property = Property.Create(PropertyType.Apartment, 54.5m, 1985, PropertyCondition.Renovated, LocationId, Apartment);

        Assert.Equal(PropertyType.Apartment, property.PropertyType);
        Assert.Equal(54.5m, property.TotalAreaM2);
        Assert.Equal(1985, property.YearBuilt);
        Assert.Equal(PropertyCondition.Renovated, property.Condition);
        Assert.Equal(LocationId, property.LocationId);
        Assert.Equal(Apartment, property.TypeSpecificAttributes);
        Assert.Empty(property.Amenities);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_WithNonPositiveArea_Throws(decimal area)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Property.Create(PropertyType.Apartment, area, null, null, LocationId, Apartment));
    }

    [Fact]
    public void Create_WithEmptyLocationId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Property.Create(PropertyType.Apartment, 50m, null, null, Guid.Empty, Apartment));
    }

    [Fact]
    public void Create_WithAttributesOfAnotherPropertyType_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Property.Create(PropertyType.Land, 500m, null, null, LocationId, Apartment));
    }

    [Fact]
    public void Create_LandWithYearBuiltOrCondition_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Property.Create(PropertyType.Land, 500m, 2000, null, LocationId, new LandAttributes()));
        Assert.Throws<ArgumentException>(() =>
            Property.Create(PropertyType.Land, 500m, null, PropertyCondition.New, LocationId, new LandAttributes()));
    }

    [Fact]
    public void Create_WithAmenities_DeduplicatesAndIgnoresEmptyIds()
    {
        var parking = Guid.NewGuid();
        var balcony = Guid.NewGuid();

        var property = Property.Create(
            PropertyType.Apartment, 50m, null, null, LocationId, Apartment, [parking, balcony, parking, Guid.Empty]);

        Assert.Equal(new[] { parking, balcony }.Order(), property.Amenities.Select(a => a.AmenityId).Order());
        Assert.All(property.Amenities, a => Assert.Equal(property.Id, a.PropertyId));
    }

    [Fact]
    public void UpdateDetails_ReplacesAmenitiesKeepingUnchangedOnes()
    {
        var parking = Guid.NewGuid();
        var balcony = Guid.NewGuid();
        var elevator = Guid.NewGuid();
        var property = Property.Create(PropertyType.Apartment, 50m, null, null, LocationId, Apartment, [parking, balcony]);
        var keptJoinRow = property.Amenities.Single(a => a.AmenityId == balcony);

        property.UpdateDetails(PropertyType.Apartment, 50m, null, null, Apartment, [balcony, elevator], []);

        Assert.Equal(
            new[] { balcony, elevator }.Order(),
            property.Amenities.Select(a => a.AmenityId).Order());
        Assert.Same(keptJoinRow, property.Amenities.Single(a => a.AmenityId == balcony));
    }

    [Fact]
    public void Create_WithProximities_DeduplicatesAndIgnoresEmptyIds()
    {
        var school = Guid.NewGuid();
        var park = Guid.NewGuid();

        var property = Property.Create(
            PropertyType.Land, 500m, null, null, LocationId, new LandAttributes(), proximityIds: [school, park, school, Guid.Empty]);

        Assert.Equal(new[] { school, park }.Order(), property.Proximities.Select(p => p.ProximityId).Order());
        Assert.All(property.Proximities, p => Assert.Equal(property.Id, p.PropertyId));
        Assert.Empty(property.Amenities);
    }

    [Fact]
    public void UpdateDetails_ReplacesProximitiesKeepingUnchangedOnesAndLeavingAmenitiesAlone()
    {
        var parking = Guid.NewGuid();
        var school = Guid.NewGuid();
        var park = Guid.NewGuid();
        var bank = Guid.NewGuid();
        var property = Property.Create(PropertyType.Apartment, 50m, null, null, LocationId, Apartment, [parking], [school, park]);
        var keptJoinRow = property.Proximities.Single(p => p.ProximityId == park);

        property.UpdateDetails(PropertyType.Apartment, 50m, null, null, Apartment, [parking], [park, bank]);

        Assert.Equal(new[] { park, bank }.Order(), property.Proximities.Select(p => p.ProximityId).Order());
        Assert.Same(keptJoinRow, property.Proximities.Single(p => p.ProximityId == park));
        Assert.Equal(parking, Assert.Single(property.Amenities).AmenityId);
    }

    [Fact]
    public void UpdateDetails_CanChangePropertyTypeTogetherWithMatchingAttributes()
    {
        var property = Property.Create(PropertyType.Apartment, 50m, 1990, null, LocationId, Apartment);
        var house = new HouseAttributes(Rooms: 4, HouseFloors: 2);

        property.UpdateDetails(PropertyType.House, 120m, 2010, PropertyCondition.New, house, [], []);

        Assert.Equal(PropertyType.House, property.PropertyType);
        Assert.Equal(house, property.TypeSpecificAttributes);
        Assert.Equal(LocationId, property.LocationId);
    }

    [Fact]
    public void UpdateDetails_WithMismatchedAttributes_ThrowsAndLeavesPropertyUnchanged()
    {
        var property = Property.Create(PropertyType.Apartment, 50m, null, null, LocationId, Apartment);

        Assert.Throws<ArgumentException>(() =>
            property.UpdateDetails(PropertyType.House, 50m, null, null, Apartment, [], []));

        Assert.Equal(PropertyType.Apartment, property.PropertyType);
    }

    [Fact]
    public void EmptyFor_ReturnsTheSchemaMatchingEachPropertyType()
    {
        foreach (var type in Enum.GetValues<PropertyType>())
        {
            Assert.Equal(type, PropertyAttributes.EmptyFor(type).GetPropertyType());
        }
    }
}
