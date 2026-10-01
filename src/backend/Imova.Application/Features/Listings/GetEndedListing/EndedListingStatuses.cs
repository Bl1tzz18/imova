using Imova.Domain.Listings;

namespace Imova.Application.Features.Listings.GetEndedListing;

// A listing that was public and no longer is, for an ordinary reason: sold, rented, expired, or
// taken down by its owner. (Suspended — a moderation decision — is not among them: it stays hidden.)
public static class EndedListingStatuses
{
    public static readonly ListingStatus[] All =
        [ListingStatus.Sold, ListingStatus.Rented, ListingStatus.Expired, ListingStatus.Archived];
}
