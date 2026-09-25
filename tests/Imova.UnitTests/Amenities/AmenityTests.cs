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
    [InlineData("electricity", PropertyType.Garage)]
    [InlineData("remote_gate", PropertyType.Garage)]
    [InlineData("kitchen_access", PropertyType.Room)]
    [InlineData("parking", PropertyType.Commercial)]
    public void TypeSpecificAmenities_ApplyWhereTheyBelong(string key, PropertyType propertyType)
    {
        Assert.True(Seeded(key).AppliesTo(propertyType));
    }

    [Theory]
    [InlineData("near_water")]
    [InlineData("near_forest")]
    [InlineData("guarded")]
    public void RemovedAmenities_AreNotSeeded(string removedKey)
    {
        // What a property is near is a Proximity now (see ProximityConfiguration); "guarded" was
        // dropped outright.
        Assert.DoesNotContain(AmenityConfiguration.Seed, a => a.Key == removedKey);
    }

    [Fact]
    public void EveryPropertyTypeButLand_HasAtLeastOneAmenity()
    {
        foreach (var type in Enum.GetValues<PropertyType>().Where(t => t != PropertyType.Land))
        {
            Assert.Contains(AmenityConfiguration.Seed, a => a.AppliesTo(type));
        }
    }

    [Fact]
    public void Land_HasNoAmenities()
    {
        // The Land form has no amenity section (detailLayouts.ts) — this would need one again.
        Assert.DoesNotContain(AmenityConfiguration.Seed, a => a.AppliesTo(PropertyType.Land));
    }

    [Fact]
    public void Garage_KeepsItsAmenities()
    {
        Assert.Equal(
            ["alarm_system", "electricity", "remote_gate", "video_surveillance"],
            AmenityConfiguration.Seed.Where(a => a.AppliesTo(PropertyType.Garage)).Select(a => a.Key).Order());
    }

    [Fact]
    public void Constructor_WithoutTypes_AppliesToEveryType()
    {
        var amenity = new Amenity(Guid.NewGuid(), "elevator", "Ascensor");

        Assert.Equal(AmenityCategory.General, amenity.Category);
        Assert.All(Enum.GetValues<PropertyType>(), t => Assert.True(amenity.AppliesTo(t)));
    }
}
