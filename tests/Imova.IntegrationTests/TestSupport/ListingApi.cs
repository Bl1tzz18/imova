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

    // An Apartment/Rent listing with every type-specific attribute filled in — tests override
    // individual keys as needed.
    public static async Task<Dictionary<string, object?>> ValidBodyAsync(HttpClient client)
    {
        var raioane = await client.GetFromJsonAsync<List<RaionDto>>("/api/v1/locations/raioane");
        return new()
        {
            ["propertyType"] = "Apartment",
            ["totalAreaM2"] = 54,
            ["yearBuilt"] = 1985,
            // Apartment uses typeSpecificAttributes.finishCondition, not the general condition.
            ["condition"] = null,
            ["typeSpecificAttributes"] = CompleteAttributes("Apartment"),
            ["amenityIds"] = Array.Empty<Guid>(),
            ["proximityIds"] = Array.Empty<Guid>(),
            ["country"] = "Moldova",
            ["raionId"] = raioane!.First().Id,
            ["streetAddress"] = "Str. Ismail",
            ["transactionType"] = "Rent",
            ["title"] = "Apartament 2 camere",
            ["description"] = "Apartament luminos, aproape de centru.",
            ["price"] = 550,
            ["currency"] = "EUR",
            ["isNegotiable"] = false,
            ["rentalDetails"] = new Dictionary<string, object?> { ["petsAllowed"] = true, ["minLeasePeriodMonths"] = 12 },
        };
    }

    // Every detail the listing form collects for each PropertyType — including the conditional
    // fields their controlling values unlock (boiler heating details, the agricultural soil score,
    // the office count).
    public static Dictionary<string, object?> CompleteAttributes(string propertyType) => propertyType switch
    {
        "Apartment" => new()
        {
            ["housingStockType"] = "NewConstruction", ["buildingMaterial"] = "Monolith", ["finishCondition"] = "EuroRenovated",
            ["layout"] = "IndividualLayout", ["rooms"] = 2, ["floor"] = 3, ["totalFloors"] = 9, ["bathrooms"] = 1,
            ["livingAreaM2"] = 38.5, ["kitchenAreaM2"] = 12,
            ["heatingSystem"] = "OwnBoiler", ["heatingEnergySource"] = "Gas", ["heatingDistribution"] = "Radiators",
            ["gasSupply"] = true, ["floorMaterial"] = "Laminate",
        },
        "House" => new()
        {
            ["rooms"] = 5, ["houseType"] = "Duplex", ["buildingMaterial"] = "LimestoneBlock", ["finishCondition"] = "EuroRenovated",
            ["houseFloors"] = 2, ["ceilingHeightM"] = 2.8,
            ["livingAreaM2"] = 150, ["landAreaM2"] = 600, ["kitchenAreaM2"] = 20, ["atticAreaM2"] = 35, ["basementAreaM2"] = 25,
            ["heatingSystem"] = "OwnBoiler", ["heatingEnergySource"] = "Gas", ["heatingDistribution"] = "UnderfloorHeating",
            ["waterSupply"] = "DrilledWell", ["sewerage"] = "SepticTank", ["gasSupply"] = true,
            ["floorMaterial"] = "Parquet", ["atticMaterial"] = "Osb", ["roofMaterial"] = "Tile", ["windowType"] = "Thermopane",
        },
        "Land" => new()
        {
            ["plotType"] = "Agricultural", ["locationContext"] = "OutsideTownLimits", ["soilQualityScore"] = 64,
            ["roadAccess"] = "Gravel", ["gasPipelineAtBoundary"] = false, ["electricitySupplyAtBoundary"] = true,
            ["sewerageAtBoundary"] = false, ["irrigationSystem"] = true, ["phoneLineAvailable"] = false,
        },
        "Commercial" => new()
        {
            ["spaceType"] = "OfficeSpace", ["finishCondition"] = "CosmeticRepair", ["floor"] = -1, ["totalFloorsInBuilding"] = 5,
            ["workingAreaM2"] = 110, ["numberOfOffices"] = 6,
            ["bathrooms"] = 2, ["phoneLinesCount"] = 4, ["mainStreetAccess"] = true, ["electricalPower"] = "three-phase 380V",
            ["gasSupply"] = false,
        },
        "Garage" => new() { ["parkingType"] = "UndergroundParking" },
        "Room" => new() { ["bathroomType"] = "Shared", ["roommateCount"] = 2 },
        _ => throw new ArgumentOutOfRangeException(nameof(propertyType)),
    };
}
