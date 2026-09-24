using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.ArchiveListing;

// "Deactivate": Active/Rented/Sold/Expired -> Archived. A listing that never went live is
// discarded via DeleteListing instead.
// RequestingUserId/IsAdmin are always supplied by the endpoint from the caller's JWT claims —
// never trust these from the request body.
public record ArchiveListingCommand(Guid Id, Guid RequestingUserId, bool IsAdmin) : IRequest<ListingDto?>;
