using Imova.Application.Features.Listings.GetMyListings;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class GetMyListingsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsListingsFromAllOfTheUsersPublishers_InEveryStatus()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var individual = ListingTestData.AddIndividualPublisher(dbContext, userId);
        var agency = ListingTestData.AddAgencyPublisher(dbContext, userId);
        var someoneElse = ListingTestData.AddIndividualPublisher(dbContext);
        var draft = ListingTestData.AddListing(dbContext, individual.Id);
        var archived = ListingTestData.AddListing(dbContext, agency.Id).MoveTo(ListingStatus.Archived);
        ListingTestData.AddListing(dbContext, someoneElse.Id).MoveTo(ListingStatus.Active);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await new GetMyListingsHandler(dbContext, new FakeBlobStorageService())
            .Handle(new GetMyListingsQuery(userId), CancellationToken.None);

        Assert.Equal(new[] { draft.Id, archived.Id }.Order(), result.Select(l => l.Id).Order());
    }

    [Fact]
    public async Task Handle_OrdersMostRecentlyCreatedFirst()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var publisher = ListingTestData.AddIndividualPublisher(dbContext, userId);
        var older = ListingTestData.AddListing(dbContext, publisher.Id);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        await Task.Delay(10);
        var newer = ListingTestData.AddListing(dbContext, publisher.Id);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await new GetMyListingsHandler(dbContext, new FakeBlobStorageService())
            .Handle(new GetMyListingsQuery(userId), CancellationToken.None);

        Assert.Equal([newer.Id, older.Id], result.Select(l => l.Id));
    }

    [Fact]
    public async Task Handle_ForUserWithoutListings_ReturnsEmptyList()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await new GetMyListingsHandler(dbContext, new FakeBlobStorageService())
            .Handle(new GetMyListingsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(result);
    }
}
