using Imova.Application.Features.Listings.GetEndedListing;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class GetEndedListingHandlerTests
{
    private readonly ImovaDbContext _dbContext = TestDbContextFactory.Create();
    private readonly Guid _publisherId;

    public GetEndedListingHandlerTests()
    {
        _publisherId = ListingTestData.AddIndividualPublisher(_dbContext, Guid.NewGuid()).Id;
    }

    private async Task<Imova.Contracts.Listings.EndedListingDto?> GetAsync(TransactionType transaction, ListingStatus status)
    {
        var listing = ListingTestData.AddListing(_dbContext, _publisherId, transaction, ListingTestData.Eur(90_000m)).MoveTo(status);
        await _dbContext.SaveChangesAsync();
        return await new GetEndedListingHandler(_dbContext).Handle(new GetEndedListingQuery(listing.Id), CancellationToken.None);
    }

    [Theory]
    [InlineData(TransactionType.Sale, ListingStatus.Sold)]
    [InlineData(TransactionType.Rent, ListingStatus.Rented)]
    [InlineData(TransactionType.Rent, ListingStatus.Expired)]
    [InlineData(TransactionType.Rent, ListingStatus.Archived)]
    public async Task AnEndedListing_StillShowsWhatItWas(TransactionType transaction, ListingStatus status)
    {
        var ended = await GetAsync(transaction, status);

        Assert.NotNull(ended);
        Assert.Equal(status.ToString(), ended!.Status);
        Assert.Equal("Apartment", ended.PropertyType);
        Assert.Equal(90_000m, ended.Price.Amount);
        Assert.Equal("Chișinău", ended.RaionName);
    }

    [Theory]
    [InlineData(ListingStatus.Active)]
    [InlineData(ListingStatus.Draft)]
    [InlineData(ListingStatus.PendingReview)]
    [InlineData(ListingStatus.Rejected)]
    [InlineData(ListingStatus.Suspended)]
    public async Task AnythingElse_IsNotAnEndedListing(ListingStatus status)
    {
        Assert.Null(await GetAsync(TransactionType.Rent, status));
    }

    [Fact]
    public async Task AnUnknownId_IsNotAnEndedListing()
    {
        Assert.Null(await new GetEndedListingHandler(_dbContext).Handle(new GetEndedListingQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
