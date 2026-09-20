using Imova.Application.Common.Interfaces;

namespace Imova.IntegrationTests.TestSupport;

// Replaces the real NominatimGeocodingService in integration tests (see each test file's
// WithWebHostBuilder/ConfigureServices override) — tests must never hit the real Nominatim API:
// it's slow, network-dependent, and hammering it from a test suite runs against its usage policy.
// Defaults to returning a fixed result so tests that don't care about geocoding specifically still
// see a populated location, same as before geocoding existed.
public class StubGeocodingService : IGeocodingService
{
    public GeocodingResult? ResultToReturn { get; set; } = new(47.0105, 28.8638, "Chisinau, Moldova");

    public Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken) =>
        Task.FromResult(ResultToReturn);
}
