using Imova.Application.Features.Locations.GetRaionLocalitati;
using MediatR;

namespace Imova.Api.Features.Locations.GetRaionLocalitati;

public static class GetRaionLocalitatiEndpoint
{
    public static void MapGetRaionLocalitati(this IEndpointRouteBuilder app)
    {
        // Public (no RequireAuthorization) — static reference data needed on the public listing
        // form, same treatment as GetRaioaneEndpoint. null means the raion id doesn't exist,
        // matching GetPropertyByIdEndpoint's null-means-404 convention.
        app.MapGet("/api/v1/locations/raioane/{id:guid}/localitati", async (
            Guid id,
            HttpContext httpContext,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var localitati = await sender.Send(new GetRaionLocalitatiQuery(id), cancellationToken);
            if (localitati is null)
            {
                return Results.NotFound();
            }

            // Only the success path is cacheable — an unknown raion id isn't cached server-side
            // either (see GetRaionLocalitatiHandler), so a 404 shouldn't get frozen by browsers/CDNs.
            httpContext.Response.Headers.CacheControl = "public, max-age=86400";
            return Results.Ok(localitati);
        });
    }
}
