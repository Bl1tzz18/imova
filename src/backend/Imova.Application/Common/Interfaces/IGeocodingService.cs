namespace Imova.Application.Common.Interfaces;

// Resolves a free-text address into coordinates so listing creation/edit can derive
// PropertyLocation's lat/lng server-side instead of trusting client-supplied values — see
// CreatePropertyHandler/UpdatePropertyHandler. Never throws: a failure here (address doesn't
// resolve, provider timeout/outage) must not block saving the listing, so it returns null instead
// — mirrors IExternalImageFetcher.TryDownloadAsync's contract.
public interface IGeocodingService
{
    Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken);
}

public record GeocodingResult(double Latitude, double Longitude, string FormattedAddress);
