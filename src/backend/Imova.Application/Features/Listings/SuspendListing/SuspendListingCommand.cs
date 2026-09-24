using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.SuspendListing;

// Admin-only (enforced by SuspendListingHandler) — takes an Active listing down. IsAdmin comes
// from the caller's JWT claims; Reason from the request body.
public record SuspendListingCommand(Guid Id, bool IsAdmin, string Reason) : IRequest<ListingDto?>;
