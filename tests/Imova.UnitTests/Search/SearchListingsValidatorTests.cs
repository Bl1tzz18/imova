using Imova.Application.Features.Listings.SearchListings;
using Imova.Domain.Listings;
using Imova.Domain.Properties;

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

    // Attribute filters as the endpoint reads them from a query string.
    private static IReadOnlyList<AttributeFilter> Filters(params (string Key, string Value)[] query) =>
        AttributeFilterParser.Parse(query.GroupBy(p => p.Key).Select(g => new KeyValuePair<string, string?[]>(g.Key, g.Select(p => p.Value).ToArray())));

    [Fact]
    public void AttributeFilters_WithTheirOnePropertyType_AreValid()
    {
        Assert.Empty(ErrorsFor(new SearchListingsQuery
        {
            PropertyTypes = [PropertyType.Apartment],
            AttributeFilters = Filters(
                ("minRooms", "2"), ("maxRooms", "3"), ("minFloor", "1"), ("minBathrooms", "1"),
                ("housingStockType", "NewConstruction"), ("layout", "Studio"), ("buildingMaterial", "Brick"),
                ("heatingSystem", "OwnBoiler"), ("heatingSystem", "HeatPump"), ("gasSupply", "true"), ("maxKitchenAreaM2", "12.5")),
        }));
        Assert.Empty(ErrorsFor(new SearchListingsQuery { PropertyTypes = [PropertyType.Land], AttributeFilters = Filters(("plotType", "Forest"), ("irrigationSystem", "false")) }));
        Assert.Empty(ErrorsFor(new SearchListingsQuery { PropertyTypes = [PropertyType.Garage], AttributeFilters = Filters(("parkingType", "Garage")) }));
    }

    [Fact]
    public void AttributeFilters_WithNoneOrSeveralTypes_AreRejected()
    {
        Assert.Equal(["rooms"], ErrorsFor(new SearchListingsQuery { AttributeFilters = Filters(("minRooms", "2")) }));
        Assert.Equal(["rooms"], ErrorsFor(new SearchListingsQuery { PropertyTypes = [PropertyType.Apartment, PropertyType.House], AttributeFilters = Filters(("minRooms", "2")) }));
    }

    [Fact]
    public void AttributeFilters_ForAnotherType_AreRejected_WithAMessageNamingTheRightTypes()
    {
        var errors = _validator.Validate(new SearchListingsQuery { PropertyTypes = [PropertyType.Land], AttributeFilters = Filters(("minRooms", "2")) }).Errors;

        var error = Assert.Single(errors);
        Assert.Equal("The 'rooms' filter needs exactly one property type: Apartment or House.", error.ErrorMessage);
    }

    [Fact]
    public void AttributeRanges_MinCantExceedMax()
    {
        Assert.Equal(["rooms"], ErrorsFor(new SearchListingsQuery { PropertyTypes = [PropertyType.House], AttributeFilters = Filters(("minRooms", "5"), ("maxRooms", "3")) }));
    }

    [Fact]
    public void YearBuiltRange_And_Conditions()
    {
        Assert.Equal(["MinYearBuilt"], ErrorsFor(new SearchListingsQuery { MinYearBuilt = 2010, MaxYearBuilt = 2000 }));
        Assert.Empty(ErrorsFor(new SearchListingsQuery { MinYearBuilt = 2000, Conditions = [PropertyCondition.New, PropertyCondition.Renovated] }));
        Assert.NotEmpty(ErrorsFor(new SearchListingsQuery { Conditions = [(PropertyCondition)42] }));
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

    [Fact]
    public void MapMode_AllowsAPageOfEveryPin_ButNoMore()
    {
        Assert.Empty(ErrorsFor(new SearchListingsQuery { OnlyWithCoordinates = true, PageSize = SearchFilterRules.MaxMapResults }));
        Assert.NotEmpty(ErrorsFor(new SearchListingsQuery { OnlyWithCoordinates = true, PageSize = SearchFilterRules.MaxMapResults + 1 }));
        Assert.NotEmpty(ErrorsFor(new SearchListingsQuery { PageSize = SearchFilterRules.MaxMapResults }));
    }

    [Theory]
    [InlineData(0, 24)]
    [InlineData(1, 0)]
    [InlineData(1, 61)]
    public void Paging_IsBounded(int page, int pageSize)
    {
        Assert.NotEmpty(ErrorsFor(new SearchListingsQuery { Page = page, PageSize = pageSize }));
    }
}
