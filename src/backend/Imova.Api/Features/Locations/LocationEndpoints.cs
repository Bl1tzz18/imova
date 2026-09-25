using Imova.Api.Features.Locations.GetChisinauSectors;
using Imova.Api.Features.Locations.GetRaioane;
using Imova.Api.Features.Locations.GetRaionLocalitati;
using Imova.Api.Features.Locations.GetStreetSuggestions;
using Imova.Api.Features.Locations.SearchLocations;

namespace Imova.Api.Features.Locations;

public static class LocationEndpoints
{
    public static void MapLocationsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetRaioane();
        app.MapGetRaionLocalitati();
        app.MapGetChisinauSectors();
        app.MapGetStreetSuggestions();
        app.MapSearchLocations();
    }
}
