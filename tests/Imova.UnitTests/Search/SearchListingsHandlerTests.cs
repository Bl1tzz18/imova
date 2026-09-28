using Imova.Application.Features.Listings.SearchListings;
using Imova.Domain.Listings;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Search;

public class SearchListingsHandlerTests
{
    private sealed class FakeListingSearch(IReadOnlyList<Guid> ids, int total) : IListingSearch
    {
        public SearchListingsQuery? Received { get; private set; }

        public Task<(IReadOnlyList<Guid> ListingIds, int TotalCount)> SearchAsync(SearchListingsQuery query, CancellationToken cancellationToken)
        {
            Received = query;
            return Task.FromResult((ids, total));
        }
    }

    [Fact]
    public async Task Handle_ReturnsTheListingsInTheSearchesOrder_WithTheTotalAndPage()
    {
        var db = TestDbContextFactory.Create();
        var publisher = ListingTestData.AddIndividualPublisher(db);
        var first = ListingTestData.AddListing(db, publisher.Id).MoveTo(ListingStatus.Active);
        var second = ListingTestData.AddListing(db, publisher.Id).MoveTo(ListingStatus.Active);
        await db.SaveChangesAsync();
        var search = new FakeListingSearch([second.Id, first.Id], total: 42);

        var result = await new SearchListingsHandler(db, new FakeBlobStorageService(), search)
            .Handle(new SearchListingsQuery { Page = 3, PageSize = 2 }, CancellationToken.None);

        Assert.Equal([second.Id, first.Id], result.Items.Select(l => l.Id));
        Assert.Equal((3, 2, 42), (result.Page, result.PageSize, result.TotalCount));
        // Cards and search results never carry contact details.
        Assert.All(result.Items, l => Assert.Null(l.Contact));
    }
}
