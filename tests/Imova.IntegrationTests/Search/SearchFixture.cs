using System.Net.Http.Json;
using Imova.Contracts.Amenities;
using Imova.Contracts.Listings;
using Imova.Contracts.Locations;
using Imova.Contracts.Proximities;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Search;

// Six approved listings, created once for the whole test class, all priced inside a random EUR
// band nobody else uses — every search adds that band, so other data in the shared dev database
// never shows up in the results.
public sealed class SearchFixture : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _root = new();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public HttpClient Anonymous { get; private set; } = null!;

    public decimal BandStart { get; } = 1_000_000_000m + Random.Shared.Next(0, 8_000_000) * 1_000m;

    public decimal BandEnd => BandStart + 999m;

    public Guid RaionA { get; private set; }

    public Guid RaionB { get; private set; }

    public Guid LocalitateA { get; private set; }

    public Dictionary<string, Guid> Amenities { get; private set; } = [];

    public Dictionary<string, Guid> Proximities { get; private set; } = [];

    // Name ("A1", "A2", …) → listing id.
    public Dictionary<string, Guid> Listings { get; } = [];

    private HttpClient _owner = null!;

    public async Task InitializeAsync()
    {
        Factory = ListingApi.Configure(_root);
        Anonymous = Factory.CreateClient();
        var (owner, _) = await ListingApi.RegisterAsync(Factory);
        _owner = owner;
        var admin = await ListingApi.RegisterAdminAsync(Factory);

        var raioane = (await Anonymous.GetFromJsonAsync<List<RaionDto>>("/api/v1/locations/raioane"))!
            .Where(r => r.LocalityLabel == "Localitate").ToList();
        (RaionA, RaionB) = (raioane[0].Id, raioane[1].Id);
        LocalitateA = (await Anonymous.GetFromJsonAsync<List<LocalitateDto>>($"/api/v1/locations/raioane/{RaionA}/localitati"))![0].Id;
        Amenities = (await Anonymous.GetFromJsonAsync<List<AmenityDto>>("/api/v1/amenities"))!.ToDictionary(a => a.Key, a => a.Id);
        Proximities = (await Anonymous.GetFromJsonAsync<List<ProximityDto>>("/api/v1/proximities"))!.ToDictionary(p => p.Key, p => p.Id);

        async Task AddAsync(
            string name, string type, string transaction, decimal priceOffset, decimal area, Guid raionId,
            Action<Dictionary<string, object?>>? attributes = null, object? rental = null, Guid? localitateId = null,
            string[]? amenities = null, string[]? proximities = null)
        {
            var body = await ListingApi.ValidBodyAsync(owner);
            var attrs = ListingApi.CompleteAttributes(type);
            attributes?.Invoke(attrs);
            body["propertyType"] = type;
            body["transactionType"] = transaction;
            body["typeSpecificAttributes"] = attrs;
            body["totalAreaM2"] = area;
            body["yearBuilt"] = type == "Land" ? null : 2005;
            body["price"] = BandStart + priceOffset;
            body["currency"] = "EUR";
            body["raionId"] = raionId;
            body["localitateId"] = localitateId;
            body["rentalDetails"] = rental;
            body["amenityIds"] = (amenities ?? []).Select(k => Amenities[k]).ToArray();
            body["proximityIds"] = (proximities ?? []).Select(k => Proximities[k]).ToArray();

            var response = await owner.PostAsJsonAsync("/api/v1/listings", body);
            Assert.True(response.IsSuccessStatusCode, $"{name}: {await response.Content.ReadAsStringAsync()}");
            var listing = (await response.Content.ReadFromJsonAsync<ListingDto>())!;
            (await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null)).EnsureSuccessStatusCode();
            Listings[name] = listing.Id;
        }

        await AddAsync("A1", "Apartment", "Sale", 100, 50, RaionA,
            amenities: ["balcony", "elevator"], proximities: ["school", "park"]);
        await AddAsync("A2", "Apartment", "Rent", 200, 70, RaionA,
            attributes: a =>
            {
                a["rooms"] = 3;
                // District heating has no energy source / distribution of its own.
                a["heatingSystem"] = "DistrictHeating";
                a.Remove("heatingEnergySource");
                a.Remove("heatingDistribution");
            },
            rental: new { petsAllowed = true, utilitiesIncluded = true, minLeasePeriodMonths = 6 },
            localitateId: LocalitateA, amenities: ["balcony"]);
        await AddAsync("H1", "House", "Sale", 300, 150, RaionB, proximities: ["school"]);
        await AddAsync("L1", "Land", "Sale", 400, 1200, RaionB);
        await AddAsync("G1", "Garage", "Rent", 50, 18, RaionA,
            rental: new { utilitiesIncluded = false, minLeasePeriodMonths = 12 });
        await AddAsync("R1", "Room", "Rent", 60, 16, RaionA, rental: new { petsAllowed = false });
    }

    // The listings are Active — left behind they'd show up in the dev site's search results.
    public async Task DisposeAsync()
    {
        foreach (var id in Listings.Values)
        {
            await _owner.DeleteAsync($"/api/v1/listings/{id}");
        }

        _root.Dispose();
    }
}
