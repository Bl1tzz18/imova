using Imova.Application.Common.Interfaces;

namespace Imova.IntegrationTests.TestSupport;

// Replaces the real PhotonStreetSuggestionService in integration tests (see
// LocationsEndpointTests's WithWebHostBuilder/ConfigureServices override) — tests must never hit
// the real Photon API: it's network-dependent, and hammering it from a test suite runs against
// its "reasonable use" policy, same reasoning as StubGeocodingService for Nominatim.
public class StubStreetSuggestionService : IStreetSuggestionService
{
    public List<StreetSuggestion> ResultToReturn { get; set; } = [new("Strada Ismail", 47.0105, 28.8638)];

    public Task<List<StreetSuggestion>> SuggestStreetsAsync(string query, string? locality, CancellationToken cancellationToken) =>
        Task.FromResult(ResultToReturn);
}
