namespace Imova.Application.Features.Locations.SearchLocations;

// Same folding as the frontend's normalizeForSearch (lib/utils/search.ts): lowercase, ș/ț → s/t,
// and a/ă/â/î/i all into one bucket — Moldovan place names are spelled both ways ("Rîșcani" /
// "Râșcani") and people type either, or neither, diacritic.
public static class LocationSearchText
{
    public static string Normalize(string value) =>
        new(value.Trim().ToLowerInvariant().Select(c => c switch
        {
            'ș' or 'ş' => 's',
            'ț' or 'ţ' => 't',
            'ă' or 'â' or 'î' or 'i' => 'a',
            _ => c,
        }).ToArray());
}
