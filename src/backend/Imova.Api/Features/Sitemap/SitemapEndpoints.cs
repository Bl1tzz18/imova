using Imova.Application.Features.Sitemap;
using MediatR;

namespace Imova.Api.Features.Sitemap;

// Public, read by the web app's sitemap.xml (see GetSitemapListings / GetSitemapAgencies).
public static class SitemapEndpoints
{
    public static void MapSitemapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/sitemap/listings", async (int? page, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetSitemapListingsQuery(page ?? 1), cancellationToken)));

        app.MapGet("/api/v1/sitemap/agencies", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetSitemapAgenciesQuery(), cancellationToken)));
    }
}
