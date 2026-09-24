using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.MarkListingAsRented;

// Active rental listing -> Rented.
// RequestingUserId/IsAdmin are always supplied by the endpoint from the caller's JWT claims —
// never trust these from the request body.
public record MarkListingAsRentedCommand(Guid Id, Guid RequestingUserId, bool IsAdmin) : IRequest<ListingDto?>;
