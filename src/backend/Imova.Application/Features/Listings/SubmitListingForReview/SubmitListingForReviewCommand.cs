using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.SubmitListingForReview;

// Draft -> PendingReview, or Rejected -> PendingReview (resubmission) — see Listing.SubmitForReview.
// RequestingUserId/IsAdmin are always supplied by the endpoint from the caller's JWT claims —
// never trust these from the request body.
public record SubmitListingForReviewCommand(Guid Id, Guid RequestingUserId, bool IsAdmin) : IRequest<ListingDto?>;
