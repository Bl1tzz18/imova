using Imova.Api.Features.Properties.ApproveListing;
using Imova.Api.Features.Properties.ArchiveProperty;
using Imova.Api.Features.Properties.CreateProperty;
using Imova.Api.Features.Properties.DeleteProperty;
using Imova.Api.Features.Properties.GetMyProperties;
using Imova.Api.Features.Properties.GetPendingReviewProperties;
using Imova.Api.Features.Properties.GetProperties;
using Imova.Api.Features.Properties.GetPropertyById;
using Imova.Api.Features.Properties.MarkAsRented;
using Imova.Api.Features.Properties.MarkAsSold;
using Imova.Api.Features.Properties.ReinstateListing;
using Imova.Api.Features.Properties.RejectListing;
using Imova.Api.Features.Properties.RepublishProperty;
using Imova.Api.Features.Properties.SubmitForReview;
using Imova.Api.Features.Properties.SuspendListing;
using Imova.Api.Features.Properties.UpdateProperty;

namespace Imova.Api.Features.Properties;

public static class PropertyEndpoints
{
    public static void MapPropertiesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetProperties();
        app.MapGetPropertyById();
        app.MapGetMyProperties();
        app.MapCreateProperty();
        app.MapUpdateProperty();
        app.MapDeleteProperty();
        app.MapArchiveProperty();
        app.MapRepublishProperty();

        // Admin review lifecycle — see Property.SubmitForReview/Approve/Reject/Suspend/Reinstate.
        app.MapSubmitForReview();
        app.MapApproveListing();
        app.MapRejectListing();
        app.MapSuspendListing();
        app.MapReinstateListing();
        app.MapMarkAsRented();
        app.MapMarkAsSold();
        app.MapGetPendingReviewProperties();
    }
}
