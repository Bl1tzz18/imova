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

    [Theory]
    [InlineData(ListingStatus.Active)]
    [InlineData(ListingStatus.Suspended)]
    public async Task Handle_WithAStatus_ReturnsOnlyThatStatus_MostRecentlyChangedFirst(ListingStatus status)
    {
        await using var dbContext = TestDbContextFactory.Create();
        var publisher = ListingTestData.AddIndividualPublisher(dbContext);
        var older = ListingTestData.AddListing(dbContext, publisher.Id).MoveTo(status);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        await Task.Delay(10);
        var newer = ListingTestData.AddListing(dbContext, publisher.Id).MoveTo(status);
        ListingTestData.AddListing(dbContext, publisher.Id).MoveTo(ListingStatus.PendingReview);
        ListingTestData.AddListing(dbContext, publisher.Id).MoveTo(ListingStatus.Archived);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await new GetPendingReviewListingsHandler(dbContext, new FakeBlobStorageService())
            .Handle(new GetPendingReviewListingsQuery(true, Status: status), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal([newer.Id, older.Id], result.Items.Select(l => l.Id));
        Assert.All(result.Items, l => Assert.Equal(status.ToString(), l.Status));
    }

    [Theory]
    [InlineData(ListingStatus.Draft, false)]
    [InlineData(ListingStatus.Rejected, false)]
    [InlineData(ListingStatus.PendingReview, true)]
    [InlineData(ListingStatus.Active, true)]
    [InlineData(ListingStatus.Suspended, true)]
    public void Validator_AllowsOnlyTheModerationStatuses(ListingStatus status, bool valid)
    {
        var result = new GetPendingReviewListingsValidator().Validate(new GetPendingReviewListingsQuery(true, Status: status));

        Assert.Equal(valid, result.IsValid);
    }

    [Theory]
    [InlineData("IMOBIL grup")]
    [InlineData("office@imobil")]
    public async Task Handle_WithSearchText_MatchesThePublishersNameOrEmail_CaseInsensitively(string search)
    {
        await using var dbContext = TestDbContextFactory.Create();
        var agency = ListingTestData.AddAgencyPublisher(dbContext, Guid.NewGuid());
        var match = ListingTestData.AddListing(dbContext, agency.Id).MoveTo(ListingStatus.Active);
        ListingTestData.AddListing(dbContext, ListingTestData.AddIndividualPublisher(dbContext).Id).MoveTo(ListingStatus.Active);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await new GetPendingReviewListingsHandler(dbContext, new FakeBlobStorageService())
            .Handle(new GetPendingReviewListingsQuery(true, Status: ListingStatus.Active, Search: search), CancellationToken.None);

        Assert.Equal(match.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task Handle_WithSearchText_MatchesTheTitle()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var publisher = ListingTestData.AddIndividualPublisher(dbContext);
        ListingTestData.AddListing(dbContext, publisher.Id).MoveTo(ListingStatus.Active);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var found = await new GetPendingReviewListingsHandler(dbContext, new FakeBlobStorageService())
            .Handle(new GetPendingReviewListingsQuery(true, Status: ListingStatus.Active, Search: "  apartament 2 "), CancellationToken.None);
        var none = await new GetPendingReviewListingsHandler(dbContext, new FakeBlobStorageService())
            .Handle(new GetPendingReviewListingsQuery(true, Status: ListingStatus.Active, Search: "garaj"), CancellationToken.None);

        Assert.Equal(1, found.TotalCount);
        Assert.Equal(0, none.TotalCount);
    }

    [Theory]
    [InlineData("{0}")]
    [InlineData("https://imova.md/property/{0}?from=search")]
    public async Task Handle_WithAListingIdOrLink_FindsExactlyThatListing(string pattern)
    {
        await using var dbContext = TestDbContextFactory.Create();
        var publisher = ListingTestData.AddIndividualPublisher(dbContext);
        var wanted = ListingTestData.AddListing(dbContext, publisher.Id).MoveTo(ListingStatus.Active);
        ListingTestData.AddListing(dbContext, publisher.Id).MoveTo(ListingStatus.Active);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await new GetPendingReviewListingsHandler(dbContext, new FakeBlobStorageService())
            .Handle(
                new GetPendingReviewListingsQuery(true, Status: ListingStatus.Active, Search: string.Format(pattern, wanted.Id)),
                CancellationToken.None);

        Assert.Equal(wanted.Id, Assert.Single(result.Items).Id);
    }
}
