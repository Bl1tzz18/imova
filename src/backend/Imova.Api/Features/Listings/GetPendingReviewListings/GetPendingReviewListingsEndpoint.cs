using System.Security.Claims;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Listings.GetPendingReviewListings;
using MediatR;

namespace Imova.Api.Features.Listings.GetPendingReviewListings;

public static class GetPendingReviewListingsEndpoint
{
    public static void MapGetPendingReviewListings(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/admin/listings/pending-review", async (
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            var query = new GetPendingReviewListingsQuery(user.IsInRole(Roles.Admin), page, pageSize);
            return Results.Ok(await sender.Send(query, cancellationToken));
        }).RequireAuthorization();
    }
}
