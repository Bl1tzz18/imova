namespace Imova.Infrastructure.Geocoding;

// Bound from the "Photon" configuration section, same fallback treatment as GeocodingOptions — a
// missing section doesn't crash startup, since street suggestions are a best-effort UX
// enhancement, not something the app can't run without.
public class PhotonOptions
{
    public const string SectionName = "Photon";

    public string BaseUrl { get; set; } = "https://photon.komoot.io/api";

    public string UserAgent { get; set; } = "IMOVA/1.0 (+https://imova.md)";

    // No documented rate limit on komoot's public instance, unlike Nominatim's explicit 1 req/s
    // policy — but PhotonStreetSuggestionService throttles itself anyway (same process-wide gate
    // pattern as NominatimGeocodingService) as a good-neighbor default for a free shared service,
    // on top of the frontend's own debounce.
    public int MinRequestIntervalMilliseconds { get; set; } = 300;

    // Moldova's bounding box (min lon, min lat, max lon, max lat) — passed as Photon's `bbox`
    // param to restrict/bias suggestions to the country, since Photon has no `countrycodes`
    // equivalent to Nominatim's.
    public string MoldovaBoundingBox { get; set; } = "26.6172,45.4494,30.1596,48.4918";
}
