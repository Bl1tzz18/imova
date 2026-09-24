using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Listings.GetPendingReviewListings;
using Imova.Domain.Listings;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class GetPendingReviewListingsHandlerTests
{
    [Fact]
    public async Task Handle_ByAdmin_ReturnsOnlyPendingReviewListingsOldestFirstAndPaged()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var publisher = ListingTestData.AddIndividualPublisher(dbContext);
        var pending = new List<Listing>();
        for (var i = 0; i < 3; i++)
        {
            pending.Add(ListingTestData.AddListing(dbContext, publisher.Id).MoveTo(ListingStatus.PendingReview));
            await dbContext.SaveChangesAsync(CancellationToken.None);
            await Task.Delay(10);
        }

        ListingTestData.AddListing(dbContext, publisher.Id).MoveTo(ListingStatus.Active);
        ListingTestData.AddListing(dbContext, publisher.Id);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        var handler = new GetPendingReviewListingsHandler(dbContext, new FakeBlobStorageService());

        var page1 = await handler.Handle(new GetPendingReviewListingsQuery(true, Page: 1, PageSize: 2), CancellationToken.None);
        var page2 = await handler.Handle(new GetPendingReviewListingsQuery(true, Page: 2, PageSize: 2), CancellationToken.None);

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal([pending[0].Id, pending[1].Id], page1.Items.Select(l => l.Id));
        Assert.Equal([pending[2].Id], page2.Items.Select(l => l.Id));
    }

    [Fact]
    public async Task Handle_ByNonAdmin_ThrowsForbiddenAccessException()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            new GetPendingReviewListingsHandler(dbContext, new FakeBlobStorageService())
                .Handle(new GetPendingReviewListingsQuery(false), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithNothingPending_ReturnsEmptyPage()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await new GetPendingReviewListingsHandler(dbContext, new FakeBlobStorageService())
            .Handle(new GetPendingReviewListingsQuery(true), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }
}
