using Imova.Application.Features.Locations.GetRaioane;
using MediatR;

namespace Imova.Api.Features.Locations.GetRaioane;

public static class GetRaioaneEndpoint
{
    public static void MapGetRaioane(this IEndpointRouteBuilder app)
    {
        // Public (no RequireAuthorization) — static reference data needed on the public listing
        // form, same treatment as GetPropertiesEndpoint.
        app.MapGet("/api/v1/locations/raioane", async (HttpContext httpContext, ISender sender, CancellationToken cancellationToken) =>
        {
            var raioane = await sender.Send(new GetRaioaneQuery(), cancellationToken);
            httpContext.Response.Headers.CacheControl = "public, max-age=86400";
            return Results.Ok(raioane);
        });
    }
}
