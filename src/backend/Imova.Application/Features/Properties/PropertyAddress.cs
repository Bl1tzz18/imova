namespace Imova.Application.Features.Properties;

// Shared by CreatePropertyHandler and UpdatePropertyHandler — both geocode the same
// Street/District/City/Country fields via IGeocodingService, so the "turn these into one query
// string" step lives here instead of being duplicated (or one slice reaching into the other's
// handler).
public static class PropertyAddress
{
    // Street/District/City/Country is the finest-to-coarsest order Nominatim expects for a "q="
    // free-text query. Street is optional (free text, e.g. "Str. Ismail 44") — when omitted this
    // falls back to the same city/district-level precision as before it existed.
    public static string Compose(string? street, string? district, string city, string country) =>
        string.Join(", ", new[] { street, district, city, country }.Where(part => !string.IsNullOrWhiteSpace(part)));
}
