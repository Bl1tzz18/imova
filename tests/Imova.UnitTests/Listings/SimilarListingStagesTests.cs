using Imova.Application.Features.Listings.GetSimilarListings;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class SimilarListingStagesTests
{
    private static SimilarListingTarget Target(Guid? sectorId = null, Guid? localitateId = null, decimal priceEur = 80_000m) =>
        new(Guid.NewGuid(), TransactionType.Sale, PropertyType.Apartment, Guid.NewGuid(), sectorId, localitateId, priceEur, 2);

    [Fact]
    public void WithAnArea_StartsThere_AtPlusMinus20Percent_ThenBroadensToTheCityAndTheCountry()
    {
        var stages = SimilarListingStages.For(Target(sectorId: Guid.NewGuid()));

        Assert.Equal(
            [
                new SimilarListingStage(SameAreaOnly: true, SameCityOnly: true, PriceTolerance: 0.20m),
                new SimilarListingStage(SameAreaOnly: false, SameCityOnly: true, PriceTolerance: 0.30m),
                new SimilarListingStage(SameAreaOnly: false, SameCityOnly: true, PriceTolerance: null),
                new SimilarListingStage(SameAreaOnly: false, SameCityOnly: false, PriceTolerance: null),
            ],
            stages);
    }

    [Fact]
    public void AListingInALocality_HasThatLocalityAsItsArea()
    {
        Assert.True(Target(localitateId: Guid.NewGuid()).HasArea);
        Assert.True(SimilarListingStages.For(Target(localitateId: Guid.NewGuid()))[0].SameAreaOnly);
    }

    [Fact]
    public void WithoutAnArea_TheFirstStageIsTheCity()
    {
        var first = SimilarListingStages.For(Target())[0];

        Assert.Equal(new SimilarListingStage(SameAreaOnly: false, SameCityOnly: true, PriceTolerance: 0.20m), first);
    }

    [Fact]
    public void WithoutAPrice_SkipsThePriceStages()
    {
        var stages = SimilarListingStages.For(Target(sectorId: Guid.NewGuid(), priceEur: 0m));

        Assert.All(stages, s => Assert.Null(s.PriceTolerance));
        Assert.Equal(2, stages.Count);
    }

    [Fact]
    public void TheLastStage_IsNational()
    {
        var stages = SimilarListingStages.For(Target(sectorId: Guid.NewGuid()));

        Assert.False(stages[^1].SameAreaOnly);
        Assert.False(stages[^1].SameCityOnly);
        Assert.Null(stages[^1].PriceTolerance);
    }

    [Fact]
    public void Target_TakesTheRoomCountOnlyFromAnApartmentOrAHouse()
    {
        var listing = ListingTestData.NewListing();
        Property PropertyWith(PropertyType type, PropertyAttributes attributes) =>
            Property.Create(type, 50m, type == PropertyType.Land ? null : 2005, null, Guid.NewGuid(), attributes);

        Assert.Equal(2, SimilarListingTarget.For(listing, PropertyWith(PropertyType.Apartment, ListingTestData.TwoRoomApartment), Guid.NewGuid(), null, null).Rooms);
        Assert.Equal(5, SimilarListingTarget.For(listing, PropertyWith(PropertyType.House, new HouseAttributes(Rooms: 5)), Guid.NewGuid(), null, null).Rooms);
        Assert.Null(SimilarListingTarget.For(listing, PropertyWith(PropertyType.Garage, PropertyAttributes.EmptyFor(PropertyType.Garage)), Guid.NewGuid(), null, null).Rooms);
    }
}
