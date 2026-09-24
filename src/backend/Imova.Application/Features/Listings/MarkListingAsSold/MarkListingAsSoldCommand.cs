using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.MarkListingAsSold;

// Active sale listing -> Sold.
// RequestingUserId/IsAdmin are always supplied by the endpoint from the caller's JWT claims —
// never trust these from the request body.
public record MarkListingAsSoldCommand(Guid Id, Guid RequestingUserId, bool IsAdmin) : IRequest<ListingDto?>;
