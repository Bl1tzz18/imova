using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.RenewListing;

// The owner confirming their Active listing is still available: it stays Active for another
// Listing.ActiveMonths from now (see Listing.Renew and the Worker's ListingExpiry job).
// RequestingUserId/IsAdmin are always supplied by the endpoint from the caller's JWT claims —
// never trust these from the request body.
public record RenewListingCommand(Guid Id, Guid RequestingUserId, bool IsAdmin) : IRequest<ListingDto?>;
