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
    // User-Agent risks getting silently blocked. The default points at the live site, imova.md;
    // configuration can still override it per environment.
    public string UserAgent { get; set; } = "IMOVA/1.0 (+https://imova.md)";

    // Nominatim's policy caps usage at 1 request/second — for all of IMOVA together: SharedGeocodingService
    // reserves request slots this far apart in the database (GeocoderThrottle), shared by the API and the Worker.
    public int MinRequestIntervalMilliseconds { get; set; } = 1000;

    // How long a looked-up address is remembered (GeocodedAddresses) — places don't move.
    public int CacheDays { get; set; } = 180;

    // How long "no such place" is remembered — shorter, as the map gains streets.
    public int NotFoundCacheDays { get; set; } = 7;
}
