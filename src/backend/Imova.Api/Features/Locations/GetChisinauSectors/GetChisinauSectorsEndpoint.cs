using Imova.Application.Features.Locations.GetChisinauSectors;
using MediatR;

namespace Imova.Api.Features.Locations.GetChisinauSectors;

public static class GetChisinauSectorsEndpoint
{
    public static void MapGetChisinauSectors(this IEndpointRouteBuilder app)
    {
        // Public (no RequireAuthorization) — static reference data needed on the public listing
        // form, same treatment as GetRaioaneEndpoint.
        app.MapGet("/api/v1/locations/chisinau-sectors", async (HttpContext httpContext, ISender sender, CancellationToken cancellationToken) =>
        {
            var sectors = await sender.Send(new GetChisinauSectorsQuery(), cancellationToken);
            httpContext.Response.Headers.CacheControl = "public, max-age=86400";
            return Results.Ok(sectors);
        });
    }
}
