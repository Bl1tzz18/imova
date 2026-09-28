using Imova.Application.Features.Listings.SearchListings;

namespace Imova.UnitTests.TestSupport;

// Stands in for the real (Postgres/JSONB) search: records every query and answers with whatever
// `respond` returns for it.
internal sealed class FakeListingSearch(Func<SearchListingsQuery, (IReadOnlyList<Guid> Ids, int Total)> respond) : IListingSearch
{
    public FakeListingSearch(IReadOnlyList<Guid> ids) : this(_ => (ids, ids.Count))
    {
    }

    public List<SearchListingsQuery> Received { get; } = [];

    public Task<(IReadOnlyList<Guid> ListingIds, int TotalCount)> SearchAsync(SearchListingsQuery query, CancellationToken cancellationToken)
    {
        Received.Add(query);
        var (ids, total) = respond(query);
        return Task.FromResult((ids, total));
    }
}
