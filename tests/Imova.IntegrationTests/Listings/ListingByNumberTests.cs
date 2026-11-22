using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Imova.Contracts.Listings;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Listings;

// GET /api/v1/listings/by-number/{number} — the web page's address is /property/100015-…: the same
// answers as by id (200, 410 Gone when ended, 404 when not public or unknown).
public class ListingByNumberTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = ListingApi.Configure(factory);

    private async Task<(HttpClient Owner, HttpClient Admin, ListingDto Listing)> DraftSaleAsync()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var admin = await ListingApi.RegisterAdminAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(owner);
        body["transactionType"] = "Sale";
        body["rentalDetails"] = null;
        body["price"] = 85_000;
        var listing = (await (await owner.PostAsJsonAsync("/api/v1/listings", body)).Content.ReadFromJsonAsync<ListingDto>())!;
        return (owner, admin, listing);
    }

    [Fact]
    public async Task AnActiveListing_IsFoundByItsNumber()
    {
        var (owner, admin, listing) = await DraftSaleAsync();
        (await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null)).EnsureSuccessStatusCode();

        var response = await _factory.CreateClient().GetAsync($"/api/v1/listings/by-number/{listing.Number}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var found = await response.Content.ReadFromJsonAsync<ListingDto>();
        Assert.Equal(listing.Id, found!.Id);
        Assert.Equal(listing.Number, found.Number);

        await owner.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task AnEndedListing_Is410Gone_WithItsNumber()
    {
        var (owner, admin, listing) = await DraftSaleAsync();
        (await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null)).EnsureSuccessStatusCode();
        (await owner.PostAsync($"/api/v1/listings/{listing.Id}/mark-as-sold", null)).EnsureSuccessStatusCode();

        var response = await _factory.CreateClient().GetAsync($"/api/v1/listings/by-number/{listing.Number}");

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(listing.Number, json.GetProperty("number").GetInt64());

        await owner.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task ADraft_IsOnlyFoundByItsOwner()
    {
        var (owner, _, listing) = await DraftSaleAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await _factory.CreateClient().GetAsync($"/api/v1/listings/by-number/{listing.Number}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/v1/listings/by-number/{listing.Number}")).StatusCode);

        await owner.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task AnUnknownNumber_Is404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _factory.CreateClient().GetAsync("/api/v1/listings/by-number/1")).StatusCode);
    }
}
