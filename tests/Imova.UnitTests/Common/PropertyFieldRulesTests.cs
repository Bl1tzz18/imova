using Imova.Domain.Properties;

namespace Imova.UnitTests.Common;

public class PropertyFieldRulesTests
{
    [Theory]
    [InlineData(PropertyType.Apartment, FieldRequirement.Required)]
    [InlineData(PropertyType.House, FieldRequirement.Required)]
    [InlineData(PropertyType.Land, FieldRequirement.Required)]
    [InlineData(PropertyType.Commercial, FieldRequirement.Required)]
    [InlineData(PropertyType.Garage, FieldRequirement.Required)]
    [InlineData(PropertyType.Room, FieldRequirement.Optional)]
    public void Area_MatchesExpectedRequirement(PropertyType type, FieldRequirement expected) =>
        Assert.Equal(expected, PropertyFieldRules.Area(type));

    [Theory]
    [InlineData(PropertyType.Apartment, FieldRequirement.Required)]
    [InlineData(PropertyType.House, FieldRequirement.Required)]
    [InlineData(PropertyType.Commercial, FieldRequirement.Optional)]
    [InlineData(PropertyType.Land, FieldRequirement.Hidden)]
    [InlineData(PropertyType.Garage, FieldRequirement.Hidden)]
    [InlineData(PropertyType.Room, FieldRequirement.Hidden)]
    public void Rooms_MatchesExpectedRequirement(PropertyType type, FieldRequirement expected) =>
        Assert.Equal(expected, PropertyFieldRules.Rooms(type));

    [Theory]
    [InlineData(PropertyType.Apartment, FieldRequirement.Optional)]
    [InlineData(PropertyType.House, FieldRequirement.Optional)]
    [InlineData(PropertyType.Commercial, FieldRequirement.Optional)]
    [InlineData(PropertyType.Land, FieldRequirement.Hidden)]
    [InlineData(PropertyType.Garage, FieldRequirement.Hidden)]
    [InlineData(PropertyType.Room, FieldRequirement.Hidden)]
    public void Bathrooms_MatchesExpectedRequirement(PropertyType type, FieldRequirement expected) =>
        Assert.Equal(expected, PropertyFieldRules.Bathrooms(type));

    [Theory]
    [InlineData(PropertyType.Apartment, FieldRequirement.Required)]
    [InlineData(PropertyType.Commercial, FieldRequirement.Optional)]
    [InlineData(PropertyType.Garage, FieldRequirement.Optional)]
    [InlineData(PropertyType.Room, FieldRequirement.Optional)]
    [InlineData(PropertyType.House, FieldRequirement.Hidden)]
    [InlineData(PropertyType.Land, FieldRequirement.Hidden)]
    public void Floor_MatchesExpectedRequirement(PropertyType type, FieldRequirement expected) =>
        Assert.Equal(expected, PropertyFieldRules.Floor(type));

    [Theory]
    [InlineData(PropertyType.Apartment, FieldRequirement.Required)]
    [InlineData(PropertyType.House, FieldRequirement.Required)]
    [InlineData(PropertyType.Commercial, FieldRequirement.Optional)]
    [InlineData(PropertyType.Room, FieldRequirement.Optional)]
    [InlineData(PropertyType.Land, FieldRequirement.Hidden)]
    [InlineData(PropertyType.Garage, FieldRequirement.Hidden)]
    public void TotalFloors_MatchesExpectedRequirement(PropertyType type, FieldRequirement expected) =>
        Assert.Equal(expected, PropertyFieldRules.TotalFloors(type));

    [Theory]
    [InlineData(PropertyType.Land, FieldRequirement.Hidden)]
    [InlineData(PropertyType.Room, FieldRequirement.Hidden)]
    [InlineData(PropertyType.Apartment, FieldRequirement.Optional)]
    [InlineData(PropertyType.House, FieldRequirement.Optional)]
    [InlineData(PropertyType.Commercial, FieldRequirement.Optional)]
    [InlineData(PropertyType.Garage, FieldRequirement.Optional)]
    public void YearBuilt_MatchesExpectedRequirement(PropertyType type, FieldRequirement expected) =>
        Assert.Equal(expected, PropertyFieldRules.YearBuilt(type));

    [Theory]
    [InlineData(PropertyType.Apartment, FieldRequirement.Optional)]
    [InlineData(PropertyType.House, FieldRequirement.Optional)]
    [InlineData(PropertyType.Room, FieldRequirement.Optional)]
    [InlineData(PropertyType.Land, FieldRequirement.Hidden)]
    [InlineData(PropertyType.Commercial, FieldRequirement.Hidden)]
    [InlineData(PropertyType.Garage, FieldRequirement.Hidden)]
    public void Furnished_MatchesExpectedRequirement(PropertyType type, FieldRequirement expected) =>
        Assert.Equal(expected, PropertyFieldRules.Furnished(type));

    [Theory]
    [InlineData(PropertyType.Apartment, FieldRequirement.Optional)]
    [InlineData(PropertyType.House, FieldRequirement.Optional)]
    [InlineData(PropertyType.Commercial, FieldRequirement.Optional)]
    [InlineData(PropertyType.Land, FieldRequirement.Hidden)]
    [InlineData(PropertyType.Garage, FieldRequirement.Hidden)]
    [InlineData(PropertyType.Room, FieldRequirement.Hidden)]
    public void ParkingAvailable_MatchesExpectedRequirement(PropertyType type, FieldRequirement expected) =>
        Assert.Equal(expected, PropertyFieldRules.ParkingAvailable(type));

    [Theory]
    [InlineData(PropertyType.Apartment, FieldRequirement.Optional)]
    [InlineData(PropertyType.House, FieldRequirement.Optional)]
    [InlineData(PropertyType.Room, FieldRequirement.Optional)]
    [InlineData(PropertyType.Land, FieldRequirement.Hidden)]
    [InlineData(PropertyType.Commercial, FieldRequirement.Hidden)]
    [InlineData(PropertyType.Garage, FieldRequirement.Hidden)]
    public void PetsAllowed_ForRent_MatchesExpectedRequirement(PropertyType type, FieldRequirement expected) =>
        Assert.Equal(expected, PropertyFieldRules.PetsAllowed(type, ListingType.Rent));

    [Theory]
    [InlineData(PropertyType.Apartment)]
    [InlineData(PropertyType.House)]
    [InlineData(PropertyType.Land)]
    [InlineData(PropertyType.Commercial)]
    [InlineData(PropertyType.Garage)]
    [InlineData(PropertyType.Room)]
    public void PetsAllowed_ForSale_IsAlwaysHidden(PropertyType type) =>
        Assert.Equal(FieldRequirement.Hidden, PropertyFieldRules.PetsAllowed(type, ListingType.Sale));
}
