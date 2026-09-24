using System.Net;
using System.Net.Http.Json;
using Imova.Contracts.Listings;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Listings;

// Geocoding is swapped for StubGeocodingService so these never hit the real Nominatim API.
public class ListingGeocodingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _baseFactory;

    public ListingGeocodingTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
    }

    [Fact]
    public async Task CreateListing_WhenGeocodingResolves_StoresReturnedCoordinates()
    {
        var factory = ListingApi.Configure(_baseFactory, new StubGeocodingService { ResultToReturn = new(47.0105, 28.8638, "x") });
        var (client, _) = await ListingApi.RegisterAsync(factory);

        var response = await client.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(client));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var listing = (await response.Content.ReadFromJsonAsync<ListingDto>())!;
        Assert.Equal(47.0105, listing.Property.Location!.Latitude);
        Assert.Equal(28.8638, listing.Property.Location.Longitude);

        await client.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task CreateListing_WhenGeocodingFails_StillSucceedsWithNullCoordinates()
    {
        var factory = ListingApi.Configure(_baseFactory, new StubGeocodingService { ResultToReturn = null });
        var (client, _) = await ListingApi.RegisterAsync(factory);

        var response = await client.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(client));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var listing = (await response.Content.ReadFromJsonAsync<ListingDto>())!;
        Assert.Null(listing.Property.Location!.Latitude);

        await client.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task UpdateListing_WhenGeocodingFails_StillSucceedsWithNullCoordinates()
    {
        var stub = new StubGeocodingService { ResultToReturn = new(47.0105, 28.8638, "x") };
        var factory = ListingApi.Configure(_baseFactory, stub);
        var (client, _) = await ListingApi.RegisterAsync(factory);
        var body = await ListingApi.ValidBodyAsync(client);
        var created = (await (await client.PostAsJsonAsync("/api/v1/listings", body)).Content.ReadFromJsonAsync<ListingDto>())!;

        stub.ResultToReturn = null;
        var response = await client.PutAsJsonAsync($"/api/v1/listings/{created.Id}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<ListingDto>())!;
        Assert.Null(updated.Property.Location!.Latitude);

        await client.DeleteAsync($"/api/v1/listings/{created.Id}");
    }
}
