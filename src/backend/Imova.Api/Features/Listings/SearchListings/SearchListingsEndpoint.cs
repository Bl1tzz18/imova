using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Listings.SearchListings;
using MediatR;

namespace Imova.Api.Features.Listings.SearchListings;

// GET /api/v1/listings/search — the query string is the /search page's own (same names, see
// SearchQueryString), which is also what a saved search stores.
public static class SearchListingsEndpoint
{
    public static void MapSearchListings(this IEndpointRouteBuilder app)
    {
        // Public; a signed-in caller additionally gets IsSaved on each result.
        app.MapGet("/api/v1/listings/search", async (HttpRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(ToQuery(request, user), cancellationToken)));

        // The /map view of the same search: the same filters, but only listings with coordinates,
        // and every pin in one response (up to SearchFilterRules.MaxMapResults — TotalCount says
        // how many matched). Page/pageSize from the query string are ignored.
        app.MapGet("/api/v1/listings/search/map", async (HttpRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(
                ToQuery(request, user) with { OnlyWithCoordinates = true, Page = 1, PageSize = SearchFilterRules.MaxMapResults },
                cancellationToken)));
    }

    private static SearchListingsQuery ToQuery(HttpRequest request, ClaimsPrincipal user) =>
        SearchQueryString.Parse(request.Query.Select(kv => new KeyValuePair<string, string?[]>(kv.Key, kv.Value.ToArray()))) with
        {
            CurrentUserId = user.Identity?.IsAuthenticated == true ? user.GetUserId() : null,
        };
}
