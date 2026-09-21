using System.Net;
using System.Net.Http.Json;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Locations;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Imova.IntegrationTests.Locations;

// Requires the compose Postgres to be reachable (`docker compose up -d postgres`) — Program.cs
// runs EF migrations AND the CuatmLocationSeeder on startup, so these hit real seeded CUATM data,
// not fixtures. See HealthEndpointTests for the base pattern.
public class LocationsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly StubStreetSuggestionService _streetSuggestionStub = new();

    public LocationsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:Default",
                "Host=localhost;Port=5432;Database=imova;Username=imova;Password=imova");
            builder.ConfigureServices(services =>
            {
                // Never hit the real Photon API from the test suite — see
                // StubStreetSuggestionService's header comment.
                services.RemoveAll<IStreetSuggestionService>();
                services.AddSingleton<IStreetSuggestionService>(_streetSuggestionStub);
            });
        });
    }

    [Fact]
    public async Task GetRaioane_ReturnsAll37SeededRaioane()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/locations/raioane");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raioane = await response.Content.ReadFromJsonAsync<List<RaionDto>>();
        Assert.Equal(37, raioane!.Count);
    }

    [Fact]
    public async Task GetRaioane_ChisinauIsLabeledSector()
    {
        var client = _factory.CreateClient();

        var raioane = await client.GetFromJsonAsync<List<RaionDto>>("/api/v1/locations/raioane");

        var chisinau = raioane!.Single(r => r.NameRo == "Chișinău");
        Assert.Equal("Sector", chisinau.LocalityLabel);
    }

    [Fact]
    public async Task GetRaioane_RegularRaionIsLabeledLocalitate()
    {
        var client = _factory.CreateClient();

        var raioane = await client.GetFromJsonAsync<List<RaionDto>>("/api/v1/locations/raioane");

        var ialoveni = raioane!.Single(r => r.NameRo == "Ialoveni");
        Assert.Equal("Localitate", ialoveni.LocalityLabel);
    }

    [Fact]
    public async Task GetRaionLocalitati_ForChisinau_IncludesSectorsAndSuburbs()
    {
        var client = _factory.CreateClient();
        var raioane = await client.GetFromJsonAsync<List<RaionDto>>("/api/v1/locations/raioane");
        var chisinauId = raioane!.Single(r => r.NameRo == "Chișinău").Id;

        var response = await client.GetAsync($"/api/v1/locations/raioane/{chisinauId}/localitati");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var localitati = await response.Content.ReadFromJsonAsync<List<LocalitateDto>>();
        Assert.Contains(localitati!, l => l.NameRo == "Sectorul Botanica");
        Assert.Contains(localitati!, l => l.NameRo == "Durlești");
    }

    [Fact]
    public async Task GetRaionLocalitati_ForUnknownRaionId_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/locations/raioane/{Guid.NewGuid()}/localitati");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStreetSuggestions_WithAQuery_ReturnsSuggestionsFromTheService()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/locations/street-suggestions?query=Ismail");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var suggestions = await response.Content.ReadFromJsonAsync<List<StreetSuggestionDto>>();
        Assert.Equal("Strada Ismail", suggestions!.Single().Name);
    }

    [Fact]
    public async Task GetStreetSuggestions_WithNoQuery_ReturnsOkWithoutErroring()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/locations/street-suggestions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
