using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.SuspendListing;

// Admin-only (enforced by SuspendListingHandler) — takes an Active listing down, and closes the
// reports open on it (outcome ListingSuspended, the reason as their note). IsAdmin and AdminUserId
// come from the caller's JWT claims; Reason from the request body.
public record SuspendListingCommand(Guid Id, bool IsAdmin, Guid AdminUserId, string Reason) : IRequest<ListingDto?>;
