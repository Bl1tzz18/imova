using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.ApproveListing;

// Admin-only (enforced by ApproveListingHandler) — PendingReview -> Active. IsAdmin is always
// supplied by the endpoint from the caller's JWT claims, never trusted from the request body.
public record ApproveListingCommand(Guid Id, bool IsAdmin) : IRequest<ListingDto?>;
