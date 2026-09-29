using Imova.Api.Common;
using System.Security.Claims;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Listings.GetPendingReviewListings;
using Imova.Domain.Listings;
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
        }).RequireAdmin();

        // ?status=PendingReview|Active|Suspended — the admin moderation page's tabs; ?q= searches them.
        app.MapGet("/api/v1/admin/listings", async (
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken,
            ListingStatus status = ListingStatus.PendingReview,
            int page = 1,
            int pageSize = 20,
            string? q = null) =>
        {
            var query = new GetPendingReviewListingsQuery(user.IsInRole(Roles.Admin), page, pageSize, status, q);
            return Results.Ok(await sender.Send(query, cancellationToken));
        }).RequireAdmin();
    }
}
