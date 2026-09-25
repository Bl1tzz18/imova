using Imova.Application.Features.Locations.SearchLocations;
using MediatR;

namespace Imova.Api.Features.Locations.SearchLocations;

public static class SearchLocationsEndpoint
{
    public static void MapSearchLocations(this IEndpointRouteBuilder app)
    {
        // Public — the homepage search's location typeahead.
        app.MapGet("/api/v1/locations/search", async (HttpContext httpContext, ISender sender, CancellationToken cancellationToken, string? q, int limit = 8) =>
        {
            var matches = await sender.Send(new SearchLocationsQuery(q ?? string.Empty, limit), cancellationToken);
            httpContext.Response.Headers.CacheControl = "public, max-age=3600";
            return Results.Ok(matches);
        });
    }
}
