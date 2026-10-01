using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Imova.Contracts.ExchangeRates;
using Imova.Contracts.Listings;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Listings;

// A listing that has ended (sold, rented, expired, taken down) answers 410 Gone with a summary
// instead of 404, and keeps its similar listings; a moderated one stays a plain 404.
public class EndedListingTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = ListingApi.Configure(factory);

    private async Task<(HttpClient Owner, HttpClient Admin, Guid Id)> ActiveSaleAsync()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var admin = await ListingApi.RegisterAdminAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(owner);
        body["transactionType"] = "Sale";
        body["rentalDetails"] = null;
        body["price"] = 85_000;
        var id = (await (await owner.PostAsJsonAsync("/api/v1/listings", body)).Content.ReadFromJsonAsync<ListingDto>())!.Id;
        (await admin.PostAsync($"/api/v1/listings/{id}/approve", null)).EnsureSuccessStatusCode();
        return (owner, admin, id);
    }

    [Fact]
    public async Task ASoldListing_Is410Gone_WithASummaryButNoDetailsOrContact()
    {
        var (owner, _, id) = await ActiveSaleAsync();
        (await owner.PostAsync($"/api/v1/listings/{id}/mark-as-sold", null)).EnsureSuccessStatusCode();
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync($"/api/v1/listings/{id}");

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Sold", json.GetProperty("status").GetString());
        Assert.Equal("Apartament 2 camere", json.GetProperty("title").GetString());
        Assert.Equal(85_000m, json.GetProperty("price").GetProperty("amount").GetDecimal());
        foreach (var hidden in new[] { "description", "photos", "contact", "publisher", "property" })
        {
            Assert.False(json.TryGetProperty(hidden, out _), $"'{hidden}' must not be in an ended listing's summary.");
        }

        // Its page offers alternatives.
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync($"/api/v1/listings/{id}/similar")).StatusCode);

        // The owner still gets the whole listing.
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/v1/listings/{id}")).StatusCode);

        await owner.DeleteAsync($"/api/v1/listings/{id}");
    }

    [Fact]
    public async Task ASuspendedListing_StaysA404()
    {
        var (owner, admin, id) = await ActiveSaleAsync();
        (await admin.PostAsJsonAsync($"/api/v1/listings/{id}/suspend", new { reason = "Conținut duplicat." })).EnsureSuccessStatusCode();
        var anonymous = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/v1/listings/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/v1/listings/{id}/similar")).StatusCode);

        await owner.DeleteAsync($"/api/v1/listings/{id}");
    }

    [Fact]
    public async Task ExchangeRates_ArePublic_EuroBeingOne()
    {
        var rates = await _factory.CreateClient().GetFromJsonAsync<ExchangeRatesDto>("/api/v1/exchange-rates");

        Assert.Equal(1m, rates!.EurPerUnit["EUR"]);
        Assert.True(rates.EurPerUnit["MDL"] > 0);
        Assert.True(rates.EurPerUnit["USD"] > 0);
    }
}
