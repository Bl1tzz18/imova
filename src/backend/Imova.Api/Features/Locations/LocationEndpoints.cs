using Imova.Api.Features.Locations.GetRaioane;
using Imova.Api.Features.Locations.GetRaionLocalitati;

namespace Imova.Api.Features.Locations;

public static class LocationEndpoints
{
    public static void MapLocationsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetRaioane();
        app.MapGetRaionLocalitati();
    }
}
