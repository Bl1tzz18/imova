using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.RevealListingPhone;

// "Arată" on a listing's phone number (or a messaging-app button): the only way the full number
// leaves the API for someone other than the owner or an admin. Counts towards
// Listing.PhoneRevealCount (each visitor at most once per 24 hours, never the owner). Null — 404 —
// unless the listing is Active and has a number its owner didn't hide.
public record RevealListingPhoneCommand(Guid ListingId, Guid? UserId, bool IsAdmin, string? VisitorId) : IRequest<ListingPhoneDto?>;
