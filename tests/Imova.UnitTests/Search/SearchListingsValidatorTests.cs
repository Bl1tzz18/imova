using Imova.Application.Features.Listings.SearchListings;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;

namespace Imova.UnitTests.Search;

public class SearchListingsValidatorTests
{
    private readonly SearchListingsValidator _validator = new();

    private List<string> ErrorsFor(SearchListingsQuery query) =>
        _validator.Validate(query).Errors.Select(e => e.PropertyName).ToList();

    [Fact]
    public void NoFilters_IsValid()
    {
        Assert.Empty(ErrorsFor(new SearchListingsQuery()));
    }

    [Fact]
    public void TypeSpecificFilters_WithTheirOnePropertyType_AreValid()
    {
        Assert.Empty(ErrorsFor(new SearchListingsQuery
        {
            PropertyTypes = [PropertyType.Apartment],
            MinRooms = 2, MaxRooms = 3, MinFloor = 1, MinBathrooms = 1,
            HousingStockTypes = [HousingStockType.NewConstruction], Layouts = [ApartmentLayout.Studio],
            HeatingSystems = [HeatingSystem.OwnBoiler, HeatingSystem.HeatPump],
        }));
        Assert.Empty(ErrorsFor(new SearchListingsQuery { PropertyTypes = [PropertyType.Land], PlotTypes = [PlotType.Forest] }));
        Assert.Empty(ErrorsFor(new SearchListingsQuery { PropertyTypes = [PropertyType.Garage], ParkingTypes = [ParkingType.Garage] }));
    }

    [Fact]
    public void TypeSpecificFilters_WithNoneOrSeveralTypes_AreRejected()
    {
        Assert.Equal(["rooms"], ErrorsFor(new SearchListingsQuery { MinRooms = 2 }));
        Assert.Equal(["rooms"], ErrorsFor(new SearchListingsQuery { PropertyTypes = [PropertyType.Apartment, PropertyType.House], MinRooms = 2 }));
    }

    [Fact]
    public void TypeSpecificFilters_ForAnotherType_AreRejected_WithAMessageNamingTheRightTypes()
    {
        var errors = _validator.Validate(new SearchListingsQuery { PropertyTypes = [PropertyType.Land], MinRooms = 2 }).Errors;

        var error = Assert.Single(errors);
        Assert.Equal("The 'rooms' filter needs exactly one property type: Apartment or House.", error.ErrorMessage);
    }

    [Fact]
    public void RentalFilters_WithSale_AreRejected_ButFineOtherwise()
    {
        Assert.Contains("TransactionType", ErrorsFor(new SearchListingsQuery { TransactionType = TransactionType.Sale, PetsAllowed = true }));
        Assert.Empty(ErrorsFor(new SearchListingsQuery { TransactionType = TransactionType.Rent, PetsAllowed = true, MaxLeasePeriodMonths = 6 }));
        Assert.Empty(ErrorsFor(new SearchListingsQuery { UtilitiesIncluded = false }));
    }

    [Fact]
    public void Ranges_MinCantExceedMax()
    {
        Assert.Equal(["MinPriceEur"], ErrorsFor(new SearchListingsQuery { MinPriceEur = 100, MaxPriceEur = 50 }));
        Assert.Equal(["MinAreaM2"], ErrorsFor(new SearchListingsQuery { MinAreaM2 = 100, MaxAreaM2 = 50 }));
        Assert.Empty(ErrorsFor(new SearchListingsQuery { MinPriceEur = 50, MaxPriceEur = 50 }));
    }

    [Fact]
    public void LocalitateAndSector_AreExclusive()
    {
        Assert.Equal(["ChisinauSectorId"], ErrorsFor(new SearchListingsQuery { LocalitateId = Guid.NewGuid(), ChisinauSectorId = Guid.NewGuid() }));
    }

    [Theory]
    [InlineData(0, 24)]
    [InlineData(1, 0)]
    [InlineData(1, 61)]
    public void Paging_IsBounded(int page, int pageSize)
    {
        Assert.NotEmpty(ErrorsFor(new SearchListingsQuery { Page = page, PageSize = pageSize }));
    }

    [Fact]
    public void EveryTypeSpecificFilter_IsDetectedWhenUsed()
    {
        var all = new SearchListingsQuery
        {
            MinRooms = 1, MinFloor = 1, MinBathrooms = 1, MinLandAreaM2 = 1,
            HousingStockTypes = [HousingStockType.Existing], Layouts = [ApartmentLayout.Other], HeatingSystems = [HeatingSystem.None],
            HouseTypes = [HouseType.Villa], PlotTypes = [PlotType.Garden], LocationContexts = [LocationContext.WithinTownLimits],
            RoadAccesses = [RoadAccess.Paved], SpaceTypes = [CommercialSpaceType.Warehouse], ParkingTypes = [ParkingType.ParkingSpot],
            BathroomTypes = [BathroomType.Private],
        };

        Assert.Equal(SearchFilterRules.TypeSpecificFilters.Keys.Order(), SearchFilterRules.UsedTypeSpecificFilters(all).Order());
    }
}
