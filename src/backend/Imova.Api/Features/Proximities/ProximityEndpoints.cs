using Imova.Application.Features.Proximities.GetProximities;
using MediatR;

namespace Imova.Api.Features.Proximities;

public static class ProximityEndpoints
{
    public static void MapProximityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/proximities", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetProximitiesQuery(), cancellationToken)));
    }
}
