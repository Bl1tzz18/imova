namespace Imova.Application.Features.Properties;

// Shared by CreatePropertyHandler and UpdatePropertyHandler — both geocode the same
// Street/Sector/District/City/Country fields via IGeocodingService, so the "turn these into one
// query string" step lives here instead of being duplicated (or one slice reaching into the
// other's handler).
public static class PropertyAddress
{
    // Street/Sector/District/City/Country is the finest-to-coarsest order Nominatim expects for a
    // "q=" free-text query. Street and sector are both optional free text/names — when omitted
    // this falls back to the same city/district-level precision as before either existed. Sector
    // (an informal Chișinău neighborhood, e.g. "Botanica") sits right after street since it's a
    // well-known, OSM-recognized locality name that generally geocodes more reliably than the
    // raion name alone for an in-city Chișinău address. Callers only ever pass one of
    // sector/district at a time (mutually exclusive — a listing can't be in both a suburb and an
    // informal Chișinău neighborhood), but this function itself stays generic about that.
    public static string Compose(string? street, string? sector, string? district, string city, string country) =>
        string.Join(", ", new[] { street, sector, district, city, country }.Where(part => !string.IsNullOrWhiteSpace(part)));
}
