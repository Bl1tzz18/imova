using Imova.Api.Features.Listings.CreateListing;
using Imova.Api.Features.Listings.DeleteListing;
using Imova.Api.Features.Listings.GetListingById;
using Imova.Api.Features.Listings.GetListings;
using Imova.Api.Features.Listings.GetMyListings;
using Imova.Api.Features.Listings.GetPendingReviewListings;
using Imova.Api.Features.Listings.RejectListing;
using Imova.Api.Features.Listings.SuspendListing;
using Imova.Api.Features.Listings.Transitions;
using Imova.Api.Features.Listings.UpdateListing;

namespace Imova.Api.Features.Listings;

public static class ListingEndpoints
{
    public static void MapListingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetListings();
        app.MapGetListingById();
        app.MapGetMyListings();
        app.MapCreateListing();
        app.MapUpdateListing();
        app.MapDeleteListing();

        // Lifecycle — see Listing.SubmitForReview/Approve/Reject/Suspend/Reinstate/Archive/Publish/...
        app.MapListingTransitions();
        app.MapRejectListing();
        app.MapSuspendListing();
        app.MapGetPendingReviewListings();
    }
}
