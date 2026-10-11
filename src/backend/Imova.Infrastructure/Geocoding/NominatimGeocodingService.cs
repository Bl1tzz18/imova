using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Imova.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Imova.Infrastructure.Geocoding;

// Looks addresses up at Nominatim (OpenStreetMap's free geocoding API). Registered as a typed
// HttpClient (see Program.cs) with BaseAddress/User-Agent set from GeocodingOptions. No throttling or
// caching here: every caller goes through SharedGeocodingService, which keeps all processes together
// to Nominatim's 1 request a second and remembers answers. Never throws — a failure is
// GeocodeOutcome.Failed (logged), told apart from "no such place" so only real answers are cached.
public class NominatimGeocodingService(HttpClient httpClient, ILogger<NominatimGeocodingService> logger) : IGeocoderClient
{
    public async Task<GeocodeLookup> LookupAsync(string address, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return GeocodeLookup.NotFound;
        }

        try
        {
            var requestUri = $"search?q={Uri.EscapeDataString(address)}&format=json&limit=1&countrycodes=md";
            using var response = await httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Nominatim geocoding for \"{Address}\" returned {StatusCode}.", address, response.StatusCode);
                return GeocodeLookup.Failed;
            }

            var results = await response.Content.ReadFromJsonAsync<List<NominatimResult>>(cancellationToken);
            var match = results?.FirstOrDefault();
            if (match is null)
            {
                logger.LogInformation("Nominatim could not resolve address \"{Address}\".", address);
                return GeocodeLookup.NotFound;
            }

            if (!double.TryParse(match.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
                !double.TryParse(match.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
            {
                logger.LogWarning("Nominatim returned a non-numeric coordinate for \"{Address}\".", address);
                return GeocodeLookup.Failed;
            }

            return new GeocodeLookup(GeocodeOutcome.Found, new GeocodingResult(latitude, longitude, match.DisplayName));
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Network errors, timeouts, malformed JSON, etc. — the caller must be able to save the
            // listing regardless of a Nominatim outage.
            logger.LogWarning(ex, "Geocoding failed for address \"{Address}\".", address);
            return GeocodeLookup.Failed;
        }
    }

    private sealed record NominatimResult(
        [property: JsonPropertyName("lat")] string Lat,
        [property: JsonPropertyName("lon")] string Lon,
        [property: JsonPropertyName("display_name")] string DisplayName);
}
