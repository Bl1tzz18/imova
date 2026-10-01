using Imova.Domain.Listings;

namespace Imova.Application.Features.Listings.Visitors;

// Raises a listing's view or phone-reveal counter — unless this visitor was already counted for it
// within ListingVisitorMark.CountOncePer. One atomic statement in the database
// (Imova.Infrastructure/Listings/ListingCounters), so concurrent visits can't double count or lose
// a count. True when it counted.
public interface IListingCounters
{
    Task<bool> TryCountAsync(Guid listingId, ListingCounter counter, string visitorHash, DateTimeOffset now, CancellationToken cancellationToken);
}
