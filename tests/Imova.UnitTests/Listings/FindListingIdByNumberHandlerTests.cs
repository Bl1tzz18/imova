using Imova.Application.Features.Listings.GetListingByNumber;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class FindListingIdByNumberHandlerTests
{
    private readonly ImovaDbContext _dbContext = TestDbContextFactory.Create();

    private Task<Guid?> FindAsync(long number) =>
        new FindListingIdByNumberHandler(_dbContext).Handle(new FindListingIdByNumberQuery(number), CancellationToken.None);

    [Theory]
    [InlineData(ListingStatus.Active)]
    [InlineData(ListingStatus.Draft)]
    [InlineData(ListingStatus.Sold)]
    // Whatever its status: who may see it is GetListingById's / GetEndedListing's decision.
    public async Task FindsTheListingWithThatNumber(ListingStatus status)
    {
        var publisherId = ListingTestData.AddIndividualPublisher(_dbContext, Guid.NewGuid()).Id;
        var other = ListingTestData.AddListing(_dbContext, publisherId);
        var listing = ListingTestData.AddListing(_dbContext, publisherId, TransactionType.Sale).MoveTo(status);
        await _dbContext.SaveChangesAsync();
        Assert.NotEqual(other.Number, listing.Number);

        Assert.Equal(listing.Id, await FindAsync(listing.Number));
    }

    [Fact]
    public async Task AnUnknownNumber_FindsNothing()
    {
        Assert.Null(await FindAsync(999_999));
    }
}
