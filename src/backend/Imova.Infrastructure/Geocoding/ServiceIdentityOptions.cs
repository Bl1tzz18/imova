namespace Imova.Infrastructure.Geocoding;

// Who IMOVA is to the free OpenStreetMap services it calls (Nominatim, Photon) — bound from the
// "ServiceIdentity" configuration section, shared by the API and the Worker. Nominatim's usage
// policy (operations.osmfoundation.org/policies/nominatim) requires a User-Agent that identifies
// the application — a library default gets blocked — and asks for an email address with larger
// volumes so the operators can reach us instead of banning us; Photon (komoot) asks for fair use.
// A missing section falls back to these defaults (geocoding is best-effort, never a startup failure).
public class ServiceIdentityOptions
{
    public const string SectionName = "ServiceIdentity";

    public string AppName { get; set; } = "IMOVA";

    public string AppVersion { get; set; } = "1.0";

    public string Website { get; set; } = "https://imova.md";

    // Where the OSM operators can reach whoever runs this instance. Empty = left out.
    public string ContactEmail { get; set; } = string.Empty;

    // "IMOVA/1.0 (+https://imova.md; contact@imova.md)"
    public string UserAgent
    {
        get
        {
            var details = string.Join("; ", new[] { $"+{Website}", ContactEmail }.Where(part => !string.IsNullOrWhiteSpace(part)));
            return $"{AppName}/{AppVersion} ({details})";
        }
    }
}
