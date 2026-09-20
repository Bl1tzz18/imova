using Imova.Application.Common.Interfaces;

namespace Imova.UnitTests.TestSupport;

internal sealed class FakeGeocodingService : IGeocodingService
{
    // Defaults to a result so existing Create/UpdatePropertyHandler tests that don't care about
    // geocoding specifically still get a populated Location, matching pre-geocoding behavior.
    public GeocodingResult? ResultToReturn { get; set; } = new(47.0105, 28.8638, "Chisinau, Moldova");

    public string? LastAddressRequested { get; private set; }

    public Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken)
    {
        LastAddressRequested = address;
        return Task.FromResult(ResultToReturn);
    }
}
