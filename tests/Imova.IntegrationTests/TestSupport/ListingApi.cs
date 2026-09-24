using System.Net.Http.Headers;
using System.Net.Http.Json;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Auth;
using Imova.Contracts.Locations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Imova.IntegrationTests.TestSupport;

// Shared plumbing for the listing endpoint tests: a factory pointed at the compose Postgres with
// geocoding stubbed out, authenticated clients, and a valid create/update body.
internal static class ListingApi
{
    public const string ConnectionString = "Host=localhost;Port=5432;Database=imova;Username=imova;Password=imova";

    public static WebApplicationFactory<Program> Configure(
        WebApplicationFactory<Program> factory, StubGeocodingService? geocoding = null) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", ConnectionString);

            // Integration tests must never hit the real Nominatim API.
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGeocodingService>();
                services.AddSingleton<IGeocodingService>(geocoding ?? new StubGeocodingService());
            });
        });

    public static async Task<(HttpClient Client, AuthUserDto User)> RegisterAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var email = $"listing-test-{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "SuperSecret1",
            displayName = "Listing Test User",
            phoneNumber = "+373 69 123 456",
        });
        response.EnsureSuccessStatusCode();

        var auth = (await response.Content.ReadFromJsonAsync<AuthResultDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth.User);
    }

    // There's no UI/API for granting Admin (direct SQL only), so tests grant it through
    // UserManager and log in again to get a token carrying the role claim.
    public static async Task<HttpClient> RegisterAdminAsync(WebApplicationFactory<Program> factory)
    {
        var (_, user) = await RegisterAsync(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var appUser = (await userManager.FindByIdAsync(user.Id.ToString()))!;
            await userManager.AddToRoleAsync(appUser, Roles.Admin);
        }

        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = "SuperSecret1" });
        login.EnsureSuccessStatusCode();
        var auth = (await login.Content.ReadFromJsonAsync<AuthResultDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return client;
    }

    // An Apartment/Rent listing with its required type-specific attributes — tests override
    // individual keys as needed.
    public static async Task<Dictionary<string, object?>> ValidBodyAsync(HttpClient client)
    {
        var raioane = await client.GetFromJsonAsync<List<RaionDto>>("/api/v1/locations/raioane");
        return new()
        {
            ["propertyType"] = "Apartment",
            ["totalAreaM2"] = 54,
            ["yearBuilt"] = 1985,
            ["condition"] = "Renovated",
            ["typeSpecificAttributes"] = new Dictionary<string, object?> { ["rooms"] = 2, ["floor"] = 3, ["totalFloors"] = 9 },
            ["amenityIds"] = Array.Empty<Guid>(),
            ["country"] = "Moldova",
            ["raionId"] = raioane!.First().Id,
            ["streetAddress"] = "Str. Ismail",
            ["transactionType"] = "Rent",
            ["title"] = "Apartament 2 camere",
            ["description"] = "Apartament luminos, aproape de centru.",
            ["price"] = 550,
            ["currency"] = "EUR",
            ["isNegotiable"] = false,
            ["rentalDetails"] = new Dictionary<string, object?> { ["furnishedStatus"] = "Furnished", ["petsAllowed"] = true },
        };
    }
}
