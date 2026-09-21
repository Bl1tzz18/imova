using Imova.Contracts.Locations;
using MediatR;

namespace Imova.Application.Features.Locations.GetStreetSuggestions;

// RaionId/LocalitateId are both optional — the caller may not have picked either yet. When
// LocalitateId is absent (the common case right after picking just a Raion — Localitate is a
// separate step the user hasn't necessarily reached), GetStreetSuggestionsHandler still narrows
// the search using RaionId alone, rather than falling through to an unbiased national search (see
// its header comment) — so RaionId should be passed whenever it's known, even without a
// Localitate.
public record GetStreetSuggestionsQuery(string Query, Guid? RaionId, Guid? LocalitateId) : IRequest<List<StreetSuggestionDto>>;
