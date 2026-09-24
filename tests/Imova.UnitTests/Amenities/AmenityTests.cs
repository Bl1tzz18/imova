using Imova.Domain.Amenities;
using Imova.Domain.Properties;
using Imova.Infrastructure.Amenities;

namespace Imova.UnitTests.Amenities;

public class AmenityTests
{
    private static Amenity Seeded(string key) => AmenityConfiguration.Seed.Single(a => a.Key == key);

    [Theory]
    [InlineData(PropertyType.Apartment)]
    [InlineData(PropertyType.House)]
    [InlineData(PropertyType.Commercial)]
    [InlineData(PropertyType.Room)]
    public void Furnished_AppliesToEveryFurnishableType(PropertyType propertyType)
    {
        Assert.True(Seeded(Amenity.FurnishedKey).AppliesTo(propertyType));
    }

    [Fact]
    public void Furnished_IsInTheComfortCategory()
    {
        Assert.Equal(AmenityCategory.Comfort, Seeded(Amenity.FurnishedKey).Category);
    }

    [Fact]
    public void AnAmenity_OnlyAppliesToItsPropertyTypes()
    {
        var sauna = Seeded("sauna");

        Assert.True(sauna.AppliesTo(PropertyType.House));
        Assert.False(sauna.AppliesTo(PropertyType.Garage));
        Assert.False(sauna.AppliesTo(PropertyType.Apartment));
    }

    [Theory]
    [InlineData("pool", PropertyType.Garage)]
    [InlineData("sauna", PropertyType.Garage)]
    [InlineData("elevator", PropertyType.House)]
    [InlineData("yard", PropertyType.Apartment)]
    [InlineData("kitchen_access", PropertyType.House)]
    public void ClearlyIrrelevantAmenities_DoNotApply(string key, PropertyType propertyType)
    {
        Assert.False(Seeded(key).AppliesTo(propertyType));
    }

    [Theory]
    [InlineData("dishwasher", PropertyType.Apartment)]
    [InlineData("guarded", PropertyType.Land)]
    [InlineData("near_forest", PropertyType.Land)]
    [InlineData("electricity", PropertyType.Garage)]
    [InlineData("remote_gate", PropertyType.Garage)]
    [InlineData("kitchen_access", PropertyType.Room)]
    [InlineData("parking", PropertyType.Commercial)]
    public void TypeSpecificAmenities_ApplyWhereTheyBelong(string key, PropertyType propertyType)
    {
        Assert.True(Seeded(key).AppliesTo(propertyType));
    }

    [Fact]
    public void EveryPropertyType_HasAtLeastOneAmenity()
    {
        foreach (var type in Enum.GetValues<PropertyType>())
        {
            Assert.Contains(AmenityConfiguration.Seed, a => a.AppliesTo(type));
        }
    }

    [Fact]
    public void Constructor_WithoutTypes_AppliesToEveryType()
    {
        var amenity = new Amenity(Guid.NewGuid(), "elevator", "Ascensor");

        Assert.Equal(AmenityCategory.General, amenity.Category);
        Assert.All(Enum.GetValues<PropertyType>(), t => Assert.True(amenity.AppliesTo(t)));
    }
}
