using System.Net;
using System.Net.Http.Json;
using Imova.Contracts.Common;
using Imova.Contracts.Listings;
using Imova.Contracts.Locations;

namespace Imova.IntegrationTests.Search;

public class SearchListingsTests(SearchFixture fixture) : IClassFixture<SearchFixture>
{
    // Every search stays inside the fixture's price band, unless the query sets its own prices (which
    // must then stay inside it).
    private async Task<HttpResponseMessage> RawAsync(string query)
    {
        var band = query.Contains("PriceEur=") ? "" : $"minPriceEur={fixture.BandStart}&maxPriceEur={fixture.BandEnd}&";
        var size = query.Contains("pageSize=") ? "" : "pageSize=60&";
        return await fixture.Anonymous.GetAsync($"/api/v1/listings/search?{band}{size}{query}");
    }

    private async Task<PagedResult<ListingDto>> SearchAsync(string query = "")
    {
        var response = await RawAsync(query);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<PagedResult<ListingDto>>())!;
    }

    // The fixture names of the listings in a result, in result order.
    private async Task<List<string>> NamesAsync(string query = "")
    {
        var byId = fixture.Listings.ToDictionary(kv => kv.Value, kv => kv.Key);
        return (await SearchAsync(query)).Items.Select(l => byId[l.Id]).ToList();
    }

    private async Task AssertMatchesAsync(string query, params string[] expected) =>
        Assert.Equal(expected.Order(), (await NamesAsync(query)).Order());

    [Fact]
    public async Task NoFilters_ReturnsEveryActiveListing_WithTheTotalCount()
    {
        var result = await SearchAsync();

        Assert.Equal(6, result.TotalCount);
        Assert.Equal(6, result.Items.Count);
        Assert.Equal(1, result.Page);
    }

    [Theory]
    [InlineData("transactionType=Sale", new[] { "A1", "H1", "L1" })]
    [InlineData("transactionType=Rent", new[] { "A2", "G1", "R1" })]
    [InlineData("propertyType=Apartment", new[] { "A1", "A2" })]
    [InlineData("propertyType=Apartment&propertyType=House", new[] { "A1", "A2", "H1" })]
    [InlineData("propertyType=Garage&propertyType=Room&propertyType=Land", new[] { "G1", "R1", "L1" })]
    [InlineData("transactionType=Sale&propertyType=Apartment&propertyType=House", new[] { "A1", "H1" })]
    public async Task TransactionAndPropertyTypes(string query, string[] expected)
    {
        await AssertMatchesAsync(query, expected);
    }

    [Fact]
    public async Task PriceRange_IsInEur()
    {
        await AssertMatchesAsync(
            $"minPriceEur={fixture.BandStart + 60}&maxPriceEur={fixture.BandStart + 300}", "R1", "A1", "A2", "H1");
    }

    [Fact]
    public async Task Location_ByRaionAndByLocalitate()
    {
        await AssertMatchesAsync($"raionId={fixture.RaionA}", "A1", "A2", "G1", "R1");
        await AssertMatchesAsync($"raionId={fixture.RaionB}", "H1", "L1");
        await AssertMatchesAsync($"raionId={fixture.RaionA}&localitateId={fixture.LocalitateA}", "A2");
    }

    [Fact]
    public async Task AreaRange()
    {
        await AssertMatchesAsync("minAreaM2=50&maxAreaM2=200", "A1", "A2", "H1");
    }

    [Theory]
    [InlineData("propertyType=Apartment&minRooms=3", new[] { "A2" })]
    [InlineData("propertyType=Apartment&maxRooms=2", new[] { "A1" })]
    [InlineData("propertyType=House&minRooms=4&maxRooms=6", new[] { "H1" })]
    [InlineData("propertyType=Apartment&minFloor=3&maxFloor=3", new[] { "A1", "A2" })]
    [InlineData("propertyType=Apartment&minBathrooms=2", new string[0])]
    [InlineData("propertyType=Apartment&heatingSystem=DistrictHeating", new[] { "A2" })]
    [InlineData("propertyType=Apartment&heatingSystem=DistrictHeating&heatingSystem=OwnBoiler", new[] { "A1", "A2" })]
    [InlineData("propertyType=Apartment&housingStockType=NewConstruction&layout=IndividualLayout", new[] { "A1", "A2" })]
    [InlineData("propertyType=House&houseType=Duplex&minLandAreaM2=500", new[] { "H1" })]
    [InlineData("propertyType=House&maxLandAreaM2=500", new string[0])]
    [InlineData("propertyType=Land&plotType=Agricultural&locationContext=OutsideTownLimits&roadAccess=Gravel", new[] { "L1" })]
    [InlineData("propertyType=Land&plotType=Forest", new string[0])]
    [InlineData("propertyType=Garage&parkingType=UndergroundParking", new[] { "G1" })]
    [InlineData("propertyType=Room&bathroomType=Shared", new[] { "R1" })]
    [InlineData("propertyType=Room&bathroomType=Private", new string[0])]
    public async Task TypeSpecificFilters(string query, string[] expected)
    {
        await AssertMatchesAsync(query, expected);
    }

    [Theory]
    [InlineData("minRooms=2")]
    [InlineData("propertyType=Apartment&propertyType=House&minRooms=2")]
    [InlineData("propertyType=Land&minRooms=2")]
    [InlineData("propertyType=House&parkingType=Garage")]
    public async Task TypeSpecificFilters_NeedExactlyOneMatchingPropertyType(string query)
    {
        var response = await RawAsync(query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("needs exactly one property type", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("petsAllowed=true", new[] { "A2" })]
    [InlineData("petsAllowed=false", new[] { "R1" })]
    [InlineData("utilitiesIncluded=false", new[] { "G1", "R1" })]
    [InlineData("maxLeasePeriodMonths=6", new[] { "A2", "R1" })]
    [InlineData("maxLeasePeriodMonths=12", new[] { "A2", "G1", "R1" })]
    [InlineData("transactionType=Rent&utilitiesIncluded=true", new[] { "A2" })]
    public async Task RentalFilters_OnlyEverMatchRentals(string query, string[] expected)
    {
        await AssertMatchesAsync(query, expected);
    }

    [Fact]
    public async Task RentalFilters_WithSale_AreRejected()
    {
        var response = await RawAsync("transactionType=Sale&petsAllowed=true");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("only apply to rentals", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Amenities_RequireAllSelected_AndNeverDuplicateAListing()
    {
        var balcony = fixture.Amenities["balcony"];
        var elevator = fixture.Amenities["elevator"];

        await AssertMatchesAsync($"amenityIds={balcony}", "A1", "A2");
        var both = await SearchAsync($"amenityIds={balcony}&amenityIds={elevator}");
        Assert.Equal([fixture.Listings["A1"]], both.Items.Select(l => l.Id));
        Assert.Equal(1, both.TotalCount);
    }

    [Fact]
    public async Task Proximities_RequireAllSelected_AndNeverDuplicateAListing()
    {
        var school = fixture.Proximities["school"];
        var park = fixture.Proximities["park"];

        var bySchool = await SearchAsync($"proximityIds={school}");
        Assert.Equal(["A1", "H1"], (await NamesAsync($"proximityIds={school}")).Order());
        Assert.Equal(bySchool.Items.Count, bySchool.Items.Select(l => l.Id).Distinct().Count());
        await AssertMatchesAsync($"proximityIds={school}&proximityIds={park}", "A1");
    }

    [Fact]
    public async Task CombinedFilters()
    {
        await AssertMatchesAsync(
            $"transactionType=Sale&propertyType=Apartment&raionId={fixture.RaionA}&minAreaM2=40&amenityIds={fixture.Amenities["elevator"]}&minRooms=2",
            "A1");
    }

    [Theory]
    [InlineData("sort=PriceAsc", new[] { "G1", "R1", "A1", "A2", "H1", "L1" })]
    [InlineData("sort=PriceDesc", new[] { "L1", "H1", "A2", "A1", "R1", "G1" })]
    [InlineData("sort=AreaDesc", new[] { "L1", "H1", "A2", "A1", "G1", "R1" })]
    public async Task Sorting(string query, string[] expected)
    {
        Assert.Equal(expected, await NamesAsync(query));
    }

    [Fact]
    public async Task Paging_ReturnsOnePageAndTheTotal()
    {
        var page = (await (await RawAsync("sort=PriceAsc&pageSize=2&page=2")).Content.ReadFromJsonAsync<PagedResult<ListingDto>>())!;

        Assert.Equal(6, page.TotalCount);
        Assert.Equal((2, 2), (page.Page, page.PageSize));
        Assert.Equal([fixture.Listings["A1"], fixture.Listings["A2"]], page.Items.Select(l => l.Id));
    }

    [Theory]
    [InlineData("propertyType=Castle")]
    [InlineData("sort=Cheapest")]
    [InlineData("pageSize=500")]
    [InlineData("minAreaM2=100&maxAreaM2=50")]
    public async Task InvalidInput_Is400(string query)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await RawAsync(query)).StatusCode);
    }

    [Fact]
    public async Task NoMatches_ReturnsAnEmptyPage()
    {
        var result = await SearchAsync("propertyType=Commercial");

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Theory]
    [InlineData("chisin", "Raion", "Chișinău")]
    [InlineData("botan", "Sector", "Botanica")]
    [InlineData("riscani", "Raion", "Râșcani")]
    [InlineData("Rîșcani", "Raion", "Râșcani")]
    public async Task LocationTypeahead_MatchesWithOrWithoutDiacritics(string text, string kind, string name)
    {
        var matches = await fixture.Anonymous.GetFromJsonAsync<List<LocationSuggestionDto>>($"/api/v1/locations/search?q={text}");

        Assert.Contains(matches!, m => m.Kind == kind && m.Name == name);
    }
}
