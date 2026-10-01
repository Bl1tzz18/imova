using Imova.Application.Features.Listings.Visitors;
using Imova.Domain.Listings;

namespace Imova.UnitTests.TestSupport;

// Mimics ListingCounters: counts a (listing, counter, visitor) once per CountOncePer.
internal sealed class FakeListingCounters : IListingCounters
{
    private readonly Dictionary<(Guid, ListingCounter, string), DateTimeOffset> _marks = [];

    public List<(Guid ListingId, ListingCounter Counter)> Counted { get; } = [];

    public Task<bool> TryCountAsync(Guid listingId, ListingCounter counter, string visitorHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var key = (listingId, counter, visitorHash);
        if (_marks.TryGetValue(key, out var last) && last > now - ListingVisitorMark.CountOncePer)
        {
            return Task.FromResult(false);
        }

        _marks[key] = now;
        Counted.Add((listingId, counter));
        return Task.FromResult(true);
    }
}
