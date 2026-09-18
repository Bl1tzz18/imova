using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Imova.Contracts.Auth;
using Imova.Contracts.Properties;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Properties;

// Requires the compose Postgres to be reachable (`docker compose up -d postgres`), since
// Program.cs runs EF migrations on startup — see HealthEndpointTests.
public class PropertyOwnershipTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PropertyOwnershipTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:Default",
                "Host=localhost;Port=5432;Database=imova;Username=imova;Password=imova");
        });
    }

    private static async Task<(HttpClient Client, Guid UserId)> RegisterAuthedClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var email = $"ownership-test-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "SuperSecret1",
            displayName = "Ownership Test User",
            phoneNumber = "+373 69 123 456",
        });
        registerResponse.EnsureSuccessStatusCode();

        var auth = (await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        return (client, auth.User.Id);
    }

    // Apartment/Rent with area/rooms/floor/totalFloors set — same required-field combination as
    // CreatePropertyValidatorTests' ValidCommand(), for the same PropertyFieldRules reasons.
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
        ["latitude"] = 47.0105,
        ["longitude"] = 28.8638,
        ["area"] = 54,
        ["rooms"] = 2,
        ["floor"] = 3,
        ["totalFloors"] = 9,
    };

    private async Task<(HttpClient OwnerClient, HttpClient OtherClient, PropertyDto Property)> CreatePropertyAsOwnerAsync()
    {
        var (ownerClient, _) = await RegisterAuthedClientAsync(_factory);
        var (otherClient, _) = await RegisterAuthedClientAsync(_factory);

        var createResponse = await ownerClient.PostAsJsonAsync("/api/v1/properties", ValidCreatePropertyBody());
        createResponse.EnsureSuccessStatusCode();
        var property = (await createResponse.Content.ReadFromJsonAsync<PropertyDto>())!;

        return (ownerClient, otherClient, property);
    }

    [Fact]
    public async Task CreateProperty_DerivesOwnerIdFromJwtClaims_IgnoringAnyOwnerIdInRequestBody()
    {
        var (client, userId) = await RegisterAuthedClientAsync(_factory);
        var spoofedOwnerId = Guid.NewGuid();
        var body = ValidCreatePropertyBody();
        body["ownerId"] = spoofedOwnerId; // CreatePropertyRequest has no OwnerId field, so this must be ignored.

        var response = await client.PostAsJsonAsync("/api/v1/properties", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var property = (await response.Content.ReadFromJsonAsync<PropertyDto>())!;
        Assert.Equal(userId, property.OwnerId);
        Assert.NotEqual(spoofedOwnerId, property.OwnerId);

        await client.DeleteAsync($"/api/v1/properties/{property.Id}");
    }

    [Fact]
    public async Task CreateProperty_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/properties", ValidCreatePropertyBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProperty_ByNonOwner_Returns403AndLeavesListingUnchanged()
    {
        var (ownerClient, otherClient, property) = await CreatePropertyAsOwnerAsync();

        var response = await otherClient.PutAsJsonAsync($"/api/v1/properties/{property.Id}", new
        {
            title = "Hijacked title",
            description = property.Description,
            price = 1m,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var getResponse = await ownerClient.GetAsync($"/api/v1/properties/{property.Id}");
        var unchanged = (await getResponse.Content.ReadFromJsonAsync<PropertyDto>())!;
        Assert.Equal(property.Title, unchanged.Title);

        await ownerClient.DeleteAsync($"/api/v1/properties/{property.Id}");
    }

    [Fact]
    public async Task UpdateProperty_ByOwner_Succeeds()
    {
        var (ownerClient, _, property) = await CreatePropertyAsOwnerAsync();

        var response = await ownerClient.PutAsJsonAsync($"/api/v1/properties/{property.Id}", new
        {
            title = "Updated title",
            description = property.Description,
            price = 600m,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<PropertyDto>())!;
        Assert.Equal("Updated title", updated.Title);

        await ownerClient.DeleteAsync($"/api/v1/properties/{property.Id}");
    }

    [Fact]
    public async Task DeleteProperty_ByNonOwner_Returns403AndLeavesListingIntact()
    {
        var (ownerClient, otherClient, property) = await CreatePropertyAsOwnerAsync();

        var response = await otherClient.DeleteAsync($"/api/v1/properties/{property.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var getResponse = await ownerClient.GetAsync($"/api/v1/properties/{property.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        await ownerClient.DeleteAsync($"/api/v1/properties/{property.Id}");
    }

    [Fact]
    public async Task DeleteProperty_ByOwner_Succeeds()
    {
        var (ownerClient, _, property) = await CreatePropertyAsOwnerAsync();

        var response = await ownerClient.DeleteAsync($"/api/v1/properties/{property.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await ownerClient.GetAsync($"/api/v1/properties/{property.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
