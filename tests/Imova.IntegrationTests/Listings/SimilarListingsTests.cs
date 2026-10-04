using System.Net;
using System.Net.Http.Json;
using Imova.Contracts.Listings;
using Imova.Contracts.Locations;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Listings;

// GET /api/v1/listings/{id}/similar against real Postgres (the ranking is raw SQL). Every test works
// with garages for sale — a type/transaction nothing else in the suite publishes — in a raion picked
// at random, so other tests' listings don't get between the ones asserted on. Assertions that the
// national stage could disturb are only about relative order or absence.
public class SimilarListingsTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly List<Guid> _created = [];
    private HttpClient _owner = null!;
    private HttpClient _admin = null!;
    private HttpClient _anonymous = null!;
    private Guid _raion;
    private Guid _localitate;
    private Guid _otherLocalitate;
    private readonly decimal _price = 50_000m + Random.Shared.Next(0, 9_000);

    public SimilarListingsTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory);
    }

    public async Task InitializeAsync()
    {
        (_owner, _) = await ListingApi.RegisterAsync(_factory);
        _admin = await ListingApi.RegisterAdminAsync(_factory);
        _anonymous = _factory.CreateClient();

        // Not the first raioane: other tests' listings default to those.
        var raioane = (await _anonymous.GetFromJsonAsync<List<RaionDto>>("/api/v1/locations/raioane"))!
            .Where(r => r.LocalityLabel == "Localitate").Skip(2).ToList();
        // A random one with at least two localities (a few have only one).
        List<LocalitateDto> localitati;
        do
        {
            _raion = raioane[Random.Shared.Next(raioane.Count)].Id;
            localitati = (await _anonymous.GetFromJsonAsync<List<LocalitateDto>>($"/api/v1/locations/raioane/{_raion}/localitati"))!;
        }
        while (localitati.Count < 2);

        (_localitate, _otherLocalitate) = (localitati[0].Id, localitati[1].Id);
    }

    public async Task DisposeAsync()
    {
        foreach (var id in _created)
        {
            await _owner.DeleteAsync($"/api/v1/listings/{id}");
        }
    }

    private async Task<Guid> AddAsync(
        decimal price, Guid? localitateId, string type = "Garage", string transaction = "Sale", Guid? raionId = null, bool approve = true)
    {
        var body = await ListingApi.ValidBodyAsync(_owner);
        body["propertyType"] = type;
        body["transactionType"] = transaction;
        body["typeSpecificAttributes"] = ListingApi.CompleteAttributes(type);
        body["condition"] = type == "Garage" ? "Renovated" : null;
        body["totalAreaM2"] = 18;
        body["price"] = price;
        body["raionId"] = raionId ?? _raion;
        body["localitateId"] = localitateId;
        body["rentalDetails"] = transaction == "Rent" ? new { minLeasePeriodMonths = 12 } : null;

        var response = await _owner.PostAsJsonAsync("/api/v1/listings", body);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var id = (await response.Content.ReadFromJsonAsync<ListingDto>())!.Id;
        _created.Add(id);
        if (approve)
        {
            (await _admin.PostAsync($"/api/v1/listings/{id}/approve", null)).EnsureSuccessStatusCode();
        }

        return id;
    }

    private async Task<List<ListingDto>> SimilarAsync(Guid id)
    {
        var response = await _anonymous.GetAsync($"/api/v1/listings/{id}/similar");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<ListingDto>>())!;
    }

    [Fact]
    public async Task OnlySameTransactionAndPropertyType_OnlyActive_NeverTheListingItself()
    {
        var target = await AddAsync(_price, _localitate);
        var match = await AddAsync(_price, _localitate);
        var forRent = await AddAsync(_price, _localitate, transaction: "Rent");
        var apartment = await AddAsync(_price, _localitate, type: "Apartment");
        var pending = await AddAsync(_price, _localitate, approve: false);

        var similar = await SimilarAsync(target);

        Assert.Contains(match, similar.Select(l => l.Id));
        Assert.All(similar, l =>
        {
            Assert.Equal("Sale", l.TransactionType);
            Assert.Equal("Garage", l.Property.PropertyType);
            Assert.Equal("Active", l.Status);
        });
        Assert.DoesNotContain(target, similar.Select(l => l.Id));
        Assert.DoesNotContain(forRent, similar.Select(l => l.Id));
        Assert.DoesNotContain(apartment, similar.Select(l => l.Id));
        Assert.DoesNotContain(pending, similar.Select(l => l.Id));
        Assert.InRange(similar.Count, 1, 6);
    }

    [Fact]
    public async Task TooFewNearby_BroadensToTheCityAndAnyPrice_NearestFirst()
    {
        var target = await AddAsync(_price, _localitate);
        var sameArea = await AddAsync(_price * 1.10m, _localitate);           // stage 1: same locality, ±20%
        var sameCityNearPrice = await AddAsync(_price * 1.25m, _otherLocalitate); // stage 2: same raion, ±30%
        var sameCityAnyPrice = await AddAsync(_price * 3m, null);              // stage 3: same raion, any price

        var ids = (await SimilarAsync(target)).Select(l => l.Id).ToList();

        // The stage-1 match alone isn't enough, so the search widened; same area ranks first, then
        // the same city by closeness of price — anything further away (national) comes after.
        Assert.Equal([sameArea, sameCityNearPrice, sameCityAnyPrice], ids.Take(3));
    }

    [Fact]
    public async Task EnoughNearby_DoesNotBroaden()
    {
        var target = await AddAsync(_price, _localitate);
        var nearby = new List<Guid>();
        foreach (var factor in new[] { 0.85m, 0.95m, 1.05m, 1.15m })
        {
            nearby.Add(await AddAsync(_price * factor, _localitate));
        }

        var farPrice = await AddAsync(_price * 3m, _localitate);

        var ids = (await SimilarAsync(target)).Select(l => l.Id).ToList();

        Assert.Equal(nearby.Order(), ids.Order());
        Assert.DoesNotContain(farPrice, ids);
    }

    [Fact]
    public async Task UnknownOrInactiveListing_Returns404()
    {
        var pending = await AddAsync(_price, _localitate, approve: false);

        Assert.Equal(HttpStatusCode.NotFound, (await _anonymous.GetAsync($"/api/v1/listings/{Guid.NewGuid()}/similar")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _anonymous.GetAsync($"/api/v1/listings/{pending}/similar")).StatusCode);
    }
}
