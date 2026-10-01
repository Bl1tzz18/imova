using Imova.Application.Features.Listings.GetSimilarListings;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class GetSimilarListingsHandlerTests
{
    private readonly ImovaDbContext _dbContext = TestDbContextFactory.Create();
    private readonly Guid _publisherId;

    public GetSimilarListingsHandlerTests()
    {
        _publisherId = ListingTestData.AddIndividualPublisher(_dbContext, Guid.NewGuid()).Id;
    }

    // Answers each stage (in the order asked) with the first N of the given listings.
    private sealed class FakeFinder(IReadOnlyList<Guid> candidates, params int[] countPerStage) : ISimilarListingsFinder
    {
        public List<SimilarListingStage> Stages { get; } = [];

        public Task<IReadOnlyList<Guid>> FindAsync(
            SimilarListingTarget target, SimilarListingStage stage, int limit, CancellationToken cancellationToken)
        {
            var count = countPerStage[Math.Min(Stages.Count, countPerStage.Length - 1)];
            Stages.Add(stage);
            return Task.FromResult<IReadOnlyList<Guid>>(candidates.Take(Math.Min(count, limit)).ToList());
        }
    }

    private Listing AddActive() =>
        ListingTestData.AddListing(_dbContext, _publisherId, TransactionType.Sale, ListingTestData.Eur(80_000m)).MoveTo(ListingStatus.Active);

    private async Task<(IReadOnlyList<Imova.Contracts.Listings.ListingDto>? Result, FakeFinder Finder)> RunAsync(Guid listingId, params int[] countPerStage)
    {
        var candidates = Enumerable.Range(0, 6).Select(_ => AddActive().Id).ToList();
        await _dbContext.SaveChangesAsync();
        var finder = new FakeFinder(candidates, countPerStage);
        var result = await new GetSimilarListingsHandler(_dbContext, new FakeBlobStorageService(), finder)
            .Handle(new GetSimilarListingsQuery(listingId, null), CancellationToken.None);
        return (result, finder);
    }

    [Fact]
    public async Task EnoughAtTheFirstStage_DoesNotBroaden()
    {
        var target = AddActive();

        var (result, finder) = await RunAsync(target.Id, 4);

        Assert.Single(finder.Stages);
        Assert.Equal(4, result!.Count);
    }

    [Fact]
    public async Task TooFewResults_BroadensStageByStage_UntilThereAreEnough()
    {
        var target = AddActive();

        var (result, finder) = await RunAsync(target.Id, 1, 2, 5, 6);

        Assert.Equal(3, finder.Stages.Count);
        Assert.Equal(5, result!.Count);
    }

    [Fact]
    public async Task EvenNationallyTooFew_ReturnsWhateverItFound()
    {
        var target = AddActive();

        var (result, finder) = await RunAsync(target.Id, 0, 1, 2, 3);

        Assert.Equal(4, finder.Stages.Count);
        Assert.False(finder.Stages[^1].SameCityOnly);
        Assert.Equal(3, result!.Count);
    }

    [Fact]
    public async Task NeverMoreThanSix()
    {
        var target = AddActive();

        var (result, _) = await RunAsync(target.Id, 50);

        Assert.Equal(SimilarListingStages.MaxResults, result!.Count);
    }

    [Fact]
    public async Task KeepsTheFindersOrder()
    {
        var target = AddActive();

        var (result, _) = await RunAsync(target.Id, 6);
        var expected = _dbContext.Listings.Local.Where(l => l.Id != target.Id).Select(l => l.Id).Take(6).ToList();

        Assert.Equal(expected, result!.Select(l => l.Id));
    }

    [Fact]
    public async Task AListingThatIsntActiveOrDoesntExist_HasNoSimilarListings()
    {
        var draft = ListingTestData.AddListing(_dbContext, _publisherId);

        Assert.Null((await RunAsync(draft.Id, 6)).Result);
        Assert.Null((await RunAsync(Guid.NewGuid(), 6)).Result);
    }

    [Fact]
    public async Task ASoldListing_StillGetsSimilarOnes()
    {
        var sold = ListingTestData.AddListing(_dbContext, _publisherId, TransactionType.Sale, ListingTestData.Eur(80_000m)).MoveTo(ListingStatus.Sold);

        var (result, _) = await RunAsync(sold.Id, 6);

        Assert.Equal(6, result!.Count);
    }
}
