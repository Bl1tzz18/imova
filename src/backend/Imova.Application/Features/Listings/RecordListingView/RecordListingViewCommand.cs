using MediatR;

namespace Imova.Application.Features.Listings.RecordListingView;

// Someone opened a listing's page (sent by the page's script once it has loaded — prefetches and most
// bots don't run it). Counts towards Listing.ViewCount: an Active listing, not its owner, each
// visitor at most once per 24 hours. Silently does nothing otherwise — the caller learns nothing.
public record RecordListingViewCommand(Guid ListingId, Guid? UserId, string? VisitorId) : IRequest;
