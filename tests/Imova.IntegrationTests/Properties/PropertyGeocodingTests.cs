using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Auth;
using Imova.Contracts.Properties;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Imova.IntegrationTests.Properties;

// Requires the compose Postgres to be reachable (`docker compose up -d postgres`) — see
// HealthEndpointTests. Swaps the real NominatimGeocodingService for StubGeocodingService so these
// tests control exactly what "geocoding" returns, including simulating a failure, without ever
// touching the real Nominatim API.
public class PropertyGeocodingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _baseFactory;

    public PropertyGeocodingTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
    }

    private WebApplicationFactory<Program> CreateFactory(StubGeocodingService stub) =>
        _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:Default",
                "Host=localhost;Port=5432;Database=imova;Username=imova;Password=imova");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGeocodingService>();
                services.AddSingleton<IGeocodingService>(stub);
            });
        });

    private static async Task<HttpClient> RegisterAuthedClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var email = $"geocoding-test-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "SuperSecret1",
            displayName = "Geocoding Test User",
            phoneNumber = "+373 69 123 456",
        });
        registerResponse.EnsureSuccessStatusCode();

        var auth = (await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        return client;
    }

    private static Dictionary<string, object?> ValidCreatePropertyBody() => new()
    {
        ["title"] = "Apartament 2 camere",
        ["description"] = "Apartament luminos, aproape de centru.",
        ["propertyType"] = "Apartment",
        ["listingType"] = "Rent",
        ["price"] = 550,
        ["currency"] = "EUR",
        ["country"] = "Moldova",
        ["city"] = "Chisinau",
        ["district"] = "Botanica",
        ["area"] = 54,
        ["rooms"] = 2,
        ["floor"] = 3,
        ["totalFloors"] = 9,
    };

    [Fact]
    public async Task CreateProperty_WhenGeocodingResolves_StoresReturnedCoordinates()
    {
        var stub = new StubGeocodingService { ResultToReturn = new(47.0105, 28.8638, "Chisinau, Moldova") };
        var factory = CreateFactory(stub);
        var client = await RegisterAuthedClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/v1/properties", ValidCreatePropertyBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var property = (await response.Content.ReadFromJsonAsync<PropertyDto>())!;
        Assert.NotNull(property.Location);
        Assert.Equal(47.0105, property.Location!.Latitude);
        Assert.Equal(28.8638, property.Location.Longitude);

        await client.DeleteAsync($"/api/v1/properties/{property.Id}");
    }

    [Fact]
    public async Task CreateProperty_WhenGeocodingFails_StillSucceedsWithNullCoordinates()
    {
        // The whole point of IGeocodingService never throwing: an unresolvable address or a
        // Nominatim outage must not block the listing from being created at all.
        var stub = new StubGeocodingService { ResultToReturn = null };
        var factory = CreateFactory(stub);
        var client = await RegisterAuthedClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/v1/properties", ValidCreatePropertyBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var property = (await response.Content.ReadFromJsonAsync<PropertyDto>())!;
        Assert.NotNull(property.Location);
        Assert.Null(property.Location!.Latitude);
        Assert.Null(property.Location.Longitude);

        await client.DeleteAsync($"/api/v1/properties/{property.Id}");
    }

    [Fact]
    public async Task UpdateProperty_WhenGeocodingFails_StillSucceedsWithNullCoordinates()
    {
        var resolvingStub = new StubGeocodingService { ResultToReturn = new(47.0105, 28.8638, "Chisinau, Moldova") };
        var factory = CreateFactory(resolvingStub);
        var client = await RegisterAuthedClientAsync(factory);

        var createResponse = await client.PostAsJsonAsync("/api/v1/properties", ValidCreatePropertyBody());
        createResponse.EnsureSuccessStatusCode();
        var created = (await createResponse.Content.ReadFromJsonAsync<PropertyDto>())!;

        // Simulate the address becoming unresolvable (or Nominatim going down) on the edit itself.
        resolvingStub.ResultToReturn = null;

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/properties/{created.Id}", new
        {
            title = created.Title,
            description = created.Description,
            propertyType = created.PropertyType,
            listingType = created.ListingType,
            price = created.Price,
            currency = created.Currency,
            country = created.Location!.Country,
            city = "Somewhere Unresolvable",
            district = created.Location.District,
            area = created.Area,
            rooms = created.Rooms,
            floor = created.Floor,
            totalFloors = created.TotalFloors,
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<PropertyDto>())!;
        Assert.NotNull(updated.Location);
        Assert.Null(updated.Location!.Latitude);
        Assert.Null(updated.Location.Longitude);

        await client.DeleteAsync($"/api/v1/properties/{created.Id}");
    }
}
