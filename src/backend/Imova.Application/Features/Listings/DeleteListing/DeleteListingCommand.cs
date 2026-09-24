using MediatR;

namespace Imova.Application.Features.Listings.DeleteListing;

// RequestingUserId/IsAdmin are always supplied by the endpoint from the caller's JWT claims.
public record DeleteListingCommand(Guid Id, Guid RequestingUserId, bool IsAdmin) : IRequest<bool>;
