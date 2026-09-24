using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.ReinstateListing;

// Admin-only (enforced by ReinstateListingHandler) — lifts a suspension, Suspended -> Active.
public record ReinstateListingCommand(Guid Id, bool IsAdmin) : IRequest<ListingDto?>;
