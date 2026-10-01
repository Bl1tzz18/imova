using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Listings.GetSimilarListings;
using MediatR;

namespace Imova.Api.Features.Listings.GetSimilarListings;

public static class GetSimilarListingsEndpoint
{
    // Public, like the listing itself: 404 unless the listing exists and is Active.
    public static void MapGetSimilarListings(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/listings/{id:guid}/similar", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var currentUserId = user.Identity?.IsAuthenticated == true ? user.GetUserId() : (Guid?)null;
            var listings = await sender.Send(new GetSimilarListingsQuery(id, currentUserId), cancellationToken);
            return listings is null ? Results.NotFound() : Results.Ok(listings);
        });
    }
}
