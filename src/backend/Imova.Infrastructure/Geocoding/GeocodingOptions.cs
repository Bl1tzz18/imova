namespace Imova.Infrastructure.Geocoding;

// Bound from the "Geocoding" configuration section. Unlike BlobStorageOptions, a missing section
// doesn't crash startup (see Program.cs's GoogleAuthOptions fallback for the same pattern) —
// geocoding is a best-effort enhancement, not something the app can't run without.
public class GeocodingOptions
{
    public const string SectionName = "Geocoding";

    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    // Nominatim's usage policy requires a descriptive User-Agent identifying the application so
    // OSM can contact the operator if this integration ever causes trouble — a generic browser-ish
    // User-Agent risks getting silently blocked. Override per environment via configuration once a
    // real contact address exists.
    public string UserAgent { get; set; } = "IMOVA/1.0 (+https://imova.md)";

    // Nominatim's policy caps usage at 1 request/second — NominatimGeocodingService enforces this
    // itself via a process-wide throttle, this is just the interval it throttles to.
    public int MinRequestIntervalMilliseconds { get; set; } = 1000;
}
