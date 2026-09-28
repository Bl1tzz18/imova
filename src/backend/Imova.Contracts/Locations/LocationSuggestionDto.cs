namespace Imova.Contracts.Locations;

// One match of the location typeahead (hero search). Kind: Raion | Localitate | Sector — a Raion's
// RaionId is its own Id; a Sector belongs to Chișinău.
public record LocationSuggestionDto(string Kind, Guid Id, string Name, Guid RaionId, string RaionName);
