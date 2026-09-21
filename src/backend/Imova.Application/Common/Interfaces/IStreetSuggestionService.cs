namespace Imova.Application.Common.Interfaces;

// Suggests street names for the address-field typeahead as the user types, backed by Photon
// (komoot's open-source geocoder, built specifically for autocomplete) — unlike Nominatim (see
// IGeocodingService), which explicitly forbids autocomplete usage under its own usage policy.
// Never throws: a failure here (no matches, provider timeout/outage) must not block the form, so
// it returns an empty list instead — mirrors IGeocodingService's null-on-failure contract, just
// with an empty list since the caller here always wants *a* list to render.
public interface IStreetSuggestionService
{
    Task<List<StreetSuggestion>> SuggestStreetsAsync(string query, string? locality, CancellationToken cancellationToken);
}

public record StreetSuggestion(string Name, double? Latitude, double? Longitude);
