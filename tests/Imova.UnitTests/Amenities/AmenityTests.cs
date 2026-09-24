using Imova.Domain.Amenities;
using Imova.Domain.Listings;

namespace Imova.UnitTests.Amenities;

public class AmenityTests
{
    [Fact]
    public void Furnished_IsHiddenForRentals_BecauseRentalDetailsCarryFurnishedStatus()
    {
        Assert.False(Amenity.IsSelectableFor(Amenity.FurnishedKey, TransactionType.Rent));
    }

    [Fact]
    public void Furnished_IsSelectableForSales()
    {
        Assert.True(Amenity.IsSelectableFor(Amenity.FurnishedKey, TransactionType.Sale));
    }

    [Theory]
    [InlineData("parking")]
    [InlineData("fireplace")]
    [InlineData("pool")]
    public void OtherAmenities_AreSelectableForBothTransactionTypes(string key)
    {
        Assert.True(Amenity.IsSelectableFor(key, TransactionType.Rent));
        Assert.True(Amenity.IsSelectableFor(key, TransactionType.Sale));
    }

    [Fact]
    public void Constructor_DefaultsToTheGeneralCategory()
    {
        Assert.Equal(AmenityCategory.General, new Amenity(Guid.NewGuid(), "elevator", "Ascensor").Category);
    }
}
