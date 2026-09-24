using Imova.Domain.Amenities;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Imova.Infrastructure.Amenities;

namespace Imova.UnitTests.Amenities;

public class AmenityTests
{
    private static Amenity Seeded(string key) => AmenityConfiguration.Seed.Single(a => a.Key == key);

    [Theory]
    [InlineData(PropertyType.House)]
    [InlineData(PropertyType.Room)]
    [InlineData(PropertyType.Apartment)]
    public void Furnished_IsHiddenForRentals_BecauseRentalDetailsCarryFurnishedStatus(PropertyType propertyType)
    {
        Assert.False(Seeded(Amenity.FurnishedKey).IsSelectableFor(propertyType, TransactionType.Rent));
        Assert.True(Seeded(Amenity.FurnishedKey).IsSelectableFor(propertyType, TransactionType.Sale));
    }

    [Fact]
    public void AnAmenity_IsOnlySelectableForItsApplicablePropertyTypes()
    {
        var sauna = Seeded("sauna");

        Assert.True(sauna.IsSelectableFor(PropertyType.House, TransactionType.Sale));
        Assert.False(sauna.IsSelectableFor(PropertyType.Garage, TransactionType.Sale));
        Assert.False(sauna.IsSelectableFor(PropertyType.Apartment, TransactionType.Rent));
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
