using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Imova.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Imova.Infrastructure.Geocoding;

// Geocodes via Nominatim (OpenStreetMap's free geocoding API). Registered as a typed HttpClient
// (see Program.cs) with BaseAddress/User-Agent set from GeocodingOptions. Never throws — see
// IGeocodingService's contract; every failure path here logs a warning and returns null instead,
// mirroring ExternalImageFetcher.TryDownloadAsync.
public class NominatimGeocodingService(HttpClient httpClient, GeocodingOptions options, ILogger<NominatimGeocodingService> logger)
    : IGeocodingService
{
    // Nominatim's usage policy caps requests at 1/second. This is a process-wide gate (static, not
    // per-instance) since AddHttpClient<T> resolves a fresh NominatimGeocodingService per request
    // scope — only the HttpClient itself is pooled. Fine for "one address per listing save"; would
    // need a proper queue if this ever geocoded in bulk.
    private static readonly SemaphoreSlim ThrottleGate = new(1, 1);
    private static DateTimeOffset _lastRequestAt = DateTimeOffset.MinValue;

    public async Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        try
        {
            await ThrottleAsync(cancellationToken);

            var requestUri = $"search?q={Uri.EscapeDataString(address)}&format=json&limit=1&countrycodes=md";
            using var response = await httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Nominatim geocoding for \"{Address}\" returned {StatusCode}.", address, response.StatusCode);
                return null;
            }

            var results = await response.Content.ReadFromJsonAsync<List<NominatimResult>>(cancellationToken);
            var match = results?.FirstOrDefault();
            if (match is null)
            {
                logger.LogWarning("Nominatim could not resolve address \"{Address}\".", address);
                return null;
            }

            if (!double.TryParse(match.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
                !double.TryParse(match.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
            {
                logger.LogWarning("Nominatim returned a non-numeric coordinate for \"{Address}\".", address);
                return null;
            }

            return new GeocodingResult(latitude, longitude, match.DisplayName);
        }
        catch (Exception ex)
        {
            // Network errors, timeouts, malformed JSON, etc. — the caller must be able to save the
            // listing regardless of a Nominatim outage.
            logger.LogWarning(ex, "Geocoding failed for address \"{Address}\".", address);
            return null;
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

    private sealed record NominatimResult(
        [property: JsonPropertyName("lat")] string Lat,
        [property: JsonPropertyName("lon")] string Lon,
        [property: JsonPropertyName("display_name")] string DisplayName);
}
