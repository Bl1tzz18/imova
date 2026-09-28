using FluentValidation;
using Imova.Application.Features.Listings.SearchListings;
using Imova.Domain.Listings;
using Imova.Domain.Properties;

namespace Imova.UnitTests.Search;

public class SearchQueryStringTests
{
    [Fact]
    public void Parse_ReadsEveryKindOfFilter_CaseInsensitively()
    {
        var raion = Guid.NewGuid();
        var amenity = Guid.NewGuid();

        var query = SearchQueryString.Parse(
            $"transactiontype=rent&propertyType=Apartment&MINPRICEEUR=300&maxPriceEur=650.5&raionId={raion}" +
            $"&amenityIds={amenity}&condition=New&condition=Renovated&petsAllowed=true&minRooms=2&sort=PriceAsc&page=3");

        Assert.Equal(TransactionType.Rent, query.TransactionType);
        Assert.Equal([PropertyType.Apartment], query.PropertyTypes);
        Assert.Equal(300m, query.MinPriceEur);
        Assert.Equal(650.5m, query.MaxPriceEur);
        Assert.Equal(raion, query.RaionId);
        Assert.Equal([amenity], query.AmenityIds);
        Assert.Equal([PropertyCondition.New, PropertyCondition.Renovated], query.Conditions);
        Assert.True(query.PetsAllowed);
        Assert.Equal(ListingSort.PriceAsc, query.Sort);
        Assert.Equal(3, query.Page);
        var rooms = Assert.Single(query.AttributeFilters);
        Assert.Equal(("rooms", 2m), (rooms.Field, rooms.Min));
    }

    [Fact]
    public void Parse_DefaultsSortAndPaging()
    {
        var query = SearchQueryString.Parse("");

        Assert.Equal(ListingSort.Newest, query.Sort);
        Assert.Equal(1, query.Page);
        Assert.Equal(SearchFilterRules.DefaultPageSize, query.PageSize);
    }

    [Theory]
    [InlineData("transactionType=Lease")]
    [InlineData("transactionType=2")]
    [InlineData("minPriceEur=cheap")]
    [InlineData("raionId=not-a-guid")]
    [InlineData("petsAllowed=maybe")]
    public void Parse_WithAValueThatDoesntParse_IsAValidationError(string queryString)
    {
        var error = Assert.Throws<ValidationException>(() => SearchQueryString.Parse(queryString));

        Assert.Single(error.Errors);
    }

    [Fact]
    public void Normalize_DropsPagingAndEmptyValues_AndSortsSoTheSameSearchComparesEqual()
    {
        var a = SearchQueryString.Normalize("propertyType=House&page=4&transactionType=Sale&minPriceEur=&view=map&propertyType=Apartment");
        var b = SearchQueryString.Normalize("transactionType=Sale&propertyType=Apartment&propertyType=House");

        Assert.Equal(b, a);
        Assert.Equal("propertyType=Apartment&propertyType=House&transactionType=Sale", a);
    }

    [Fact]
    public void Normalize_KeepsValuesEncoded()
    {
        Assert.Equal("q=a%20b%26c", SearchQueryString.Normalize("q=a+b%26c"));
    }
}
