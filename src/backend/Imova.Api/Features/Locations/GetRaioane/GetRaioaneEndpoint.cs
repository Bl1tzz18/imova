using Imova.Application.Features.Locations.GetRaioane;
using MediatR;

namespace Imova.Api.Features.Locations.GetRaioane;

public static class GetRaioaneEndpoint
{
    public static void MapGetRaioane(this IEndpointRouteBuilder app)
    {
        // Public (no RequireAuthorization) — static reference data needed on the public listing
        // form, same treatment as GetPropertiesEndpoint.
        app.MapGet("/api/v1/locations/raioane", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var raioane = await sender.Send(new GetRaioaneQuery(), cancellationToken);
            return Results.Ok(raioane);
        });
    }
}
