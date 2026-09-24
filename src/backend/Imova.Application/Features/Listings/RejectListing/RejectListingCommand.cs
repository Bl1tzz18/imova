using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.RejectListing;

// Admin-only (enforced by RejectListingHandler) — PendingReview -> Rejected. IsAdmin comes from
// the caller's JWT claims; Reason from the request body (it's admin-authored feedback).
public record RejectListingCommand(Guid Id, bool IsAdmin, string Reason) : IRequest<ListingDto?>;
