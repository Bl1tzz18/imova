using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Imova.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Imova.Infrastructure.Geocoding;

// Street-name typeahead via Photon (komoot's open-source geocoder, built for autocomplete —
// Nominatim explicitly forbids that use case under its own usage policy, which is why this is a
// separate service rather than reusing NominatimGeocodingService). Registered as a typed
// HttpClient (see Program.cs) with BaseAddress/User-Agent set from PhotonOptions. Never throws —
// see IStreetSuggestionService's contract; every failure path here logs a warning and returns an
// empty list instead, mirroring NominatimGeocodingService.
public class PhotonStreetSuggestionService(HttpClient httpClient, PhotonOptions options, ILogger<PhotonStreetSuggestionService> logger)
    : IStreetSuggestionService
{
    // Same process-wide throttle pattern as NominatimGeocodingService (static, not per-instance,
    // since AddHttpClient<T> resolves a fresh instance per request scope — only the HttpClient
    // itself is pooled).
    private static readonly SemaphoreSlim ThrottleGate = new(1, 1);
    private static DateTimeOffset _lastRequestAt = DateTimeOffset.MinValue;

    public async Task<List<StreetSuggestion>> SuggestStreetsAsync(string query, string? locality, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        try
        {
            await ThrottleAsync(cancellationToken);

            // Biasing toward the already-selected Raion/Localitate/Sector: Photon has no
            // structured "restrict to this locality" param without a lat/lon centroid (which
            // Localitate/Raion don't store), so the locality name is folded straight into the
            // free-text query instead — Photon's own matching handles the rest.
            var searchText = string.IsNullOrWhiteSpace(locality) ? query : $"{query} {locality}";
            var requestUri =
                $"?q={Uri.EscapeDataString(searchText)}&limit=5&osm_tag=highway&bbox={options.MoldovaBoundingBox}";
            using var response = await httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Photon street suggestion for \"{Query}\" returned {StatusCode}.", query, response.StatusCode);
                return [];
            }

            var result = await response.Content.ReadFromJsonAsync<PhotonResponse>(cancellationToken);
            var features = result?.Features ?? [];

            return features
                .Select(f => new StreetSuggestion(
                    f.Properties.Name,
                    f.Geometry?.Coordinates is { Count: 2 } coords ? coords[1] : null,
                    f.Geometry?.Coordinates is { Count: 2 } coords2 ? coords2[0] : null))
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .DistinctBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            // Network errors, timeouts, malformed JSON, etc. — this is a typeahead aid, not
            // something that can block the form.
            logger.LogWarning(ex, "Street suggestion lookup failed for query \"{Query}\".", query);
            return [];
        }
    }

    private async Task ThrottleAsync(CancellationToken cancellationToken)
    {
        var minInterval = TimeSpan.FromMilliseconds(options.MinRequestIntervalMilliseconds);

        await ThrottleGate.WaitAsync(cancellationToken);
        try
        {
            var wait = minInterval - (DateTimeOffset.UtcNow - _lastRequestAt);
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, cancellationToken);
            }

            _lastRequestAt = DateTimeOffset.UtcNow;
        }
        finally
        {
            ThrottleGate.Release();
        }
    }

    private sealed record PhotonResponse([property: JsonPropertyName("features")] List<PhotonFeature> Features);

    private sealed record PhotonFeature(
        [property: JsonPropertyName("geometry")] PhotonGeometry? Geometry,
        [property: JsonPropertyName("properties")] PhotonProperties Properties);

    private sealed record PhotonGeometry([property: JsonPropertyName("coordinates")] List<double> Coordinates);

    // Photon's GeoJSON "properties" carries many more fields (city, state, country, osm_key,
    // ...) — only the display name is needed here, the rest is left unbound.
    private sealed record PhotonProperties([property: JsonPropertyName("name")] string Name);
}
