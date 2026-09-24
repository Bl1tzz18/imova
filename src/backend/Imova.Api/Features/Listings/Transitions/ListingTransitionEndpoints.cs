using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Listings.ApproveListing;
using Imova.Application.Features.Listings.ArchiveListing;
using Imova.Application.Features.Listings.MarkListingAsRented;
using Imova.Application.Features.Listings.MarkListingAsSold;
using Imova.Application.Features.Listings.PublishListing;
using Imova.Application.Features.Listings.ReinstateListing;
using Imova.Application.Features.Listings.SubmitListingForReview;
using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Api.Features.Listings.Transitions;

// The body-less "POST /api/v1/listings/{id}/<action>" lifecycle endpoints — each maps straight to
// one command, so they share one small mapper instead of a file apiece. (Reject/Suspend take a
// reason in the body and live in their own slices.)
public static class ListingTransitionEndpoints
{
    public static void MapListingTransitions(this IEndpointRouteBuilder app)
    {
        // Owner (or admin) actions.
        MapTransition(app, "submit-for-review", (id, user) => new SubmitListingForReviewCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin)));
        MapTransition(app, "archive", (id, user) => new ArchiveListingCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin)));
        MapTransition(app, "publish", (id, user) => new PublishListingCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin)));
        MapTransition(app, "mark-as-rented", (id, user) => new MarkListingAsRentedCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin)));
        MapTransition(app, "mark-as-sold", (id, user) => new MarkListingAsSoldCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin)));

        // Admin-only actions (enforced by their handlers).
        MapTransition(app, "approve", (id, user) => new ApproveListingCommand(id, user.IsInRole(Roles.Admin)));
        MapTransition(app, "reinstate", (id, user) => new ReinstateListingCommand(id, user.IsInRole(Roles.Admin)));
    }

    private static void MapTransition(
        IEndpointRouteBuilder app, string action, Func<Guid, ClaimsPrincipal, IRequest<ListingDto?>> createCommand)
    {
        app.MapPost($"/api/v1/listings/{{id:guid}}/{action}", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var listing = await sender.Send(createCommand(id, user), cancellationToken);
            return listing is null ? Results.NotFound() : Results.Ok(listing);
        }).RequireAuthorization();
    }
}
