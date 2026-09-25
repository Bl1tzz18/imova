using Imova.Contracts.Locations;
using MediatR;

namespace Imova.Application.Features.Locations.SearchLocations;

// Raioane, localitati and Chișinău sectors whose name matches Text — best matches first.
public record SearchLocationsQuery(string Text, int Limit = 8) : IRequest<List<LocationSuggestionDto>>;
