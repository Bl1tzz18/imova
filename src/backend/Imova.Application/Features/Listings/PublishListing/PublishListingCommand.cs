using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.PublishListing;

// The owner re-activating their own Archived/Expired listing (-> Active) without another
// review, since it was already approved once. Distinct from ApproveListing (admin, PendingReview -> Active).
// RequestingUserId/IsAdmin are always supplied by the endpoint from the caller's JWT claims —
// never trust these from the request body.
public record PublishListingCommand(Guid Id, Guid RequestingUserId, bool IsAdmin) : IRequest<ListingDto?>;
