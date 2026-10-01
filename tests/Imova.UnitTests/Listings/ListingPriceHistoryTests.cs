using Imova.Application.Features.Listings;
using Imova.Application.Features.Listings.PriceHistory;
using Imova.Domain.Listings;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class ListingPriceHistoryTests
{
    private static readonly Guid ListingId = Guid.NewGuid();
    private static readonly DateTimeOffset Published = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private static Price Eur(decimal amount) => ListingTestData.Eur(amount);

    private static Price Mdl(decimal amount) => Price.Create(amount, Currency.MDL, false, eurRate: 0.05m);

    private static ListingPriceChange Change(Price from, Price to, DateTimeOffset at) =>
        ListingPriceChange.Between(ListingId, from, to, at)!;

    private static IReadOnlyList<ListingPriceChange> Public(params ListingPriceChange[] changes) =>
        ListingPriceHistory.PublicChanges(changes, Published);

    [Fact]
    public void ADrop_ShowsTheOldPriceAndThePercent()
    {
        var changes = Public(Change(Eur(82_000), Eur(79_500), Now.AddDays(-2)));

        var reduction = ListingPriceHistory.Reduction(changes, Eur(79_500), Now);

        Assert.NotNull(reduction);
        Assert.Equal((82_000m, "EUR", 3), (reduction.PreviousAmount, reduction.PreviousCurrency, reduction.Percent));
        Assert.Equal(Now.AddDays(-2), reduction.ReducedAt);
    }

    [Fact]
    public void SeveralDropsInARow_CompareWithThePriceBeforeTheFirst()
    {
        var changes = Public(
            Change(Eur(100_000), Eur(90_000), Now.AddDays(-50)),
            Change(Eur(90_000), Eur(95_000), Now.AddDays(-20)),
            Change(Eur(95_000), Eur(92_000), Now.AddDays(-10)),
            Change(Eur(92_000), Eur(90_250), Now.AddDays(-1)));

        var reduction = ListingPriceHistory.Reduction(changes, Eur(90_250), Now)!;

        // From 95 000 (the raise ended the earlier run), not 100 000.
        Assert.Equal((95_000m, 5), (reduction.PreviousAmount, reduction.Percent));
        Assert.Equal(Now.AddDays(-1), reduction.ReducedAt);
    }

    [Fact]
    public void ARaiseAfterADrop_EndsTheBadge()
    {
        var changes = Public(
            Change(Eur(82_000), Eur(75_000), Now.AddDays(-5)),
            Change(Eur(75_000), Eur(78_000), Now.AddDays(-1)));

        Assert.Null(ListingPriceHistory.Reduction(changes, Eur(78_000), Now));
    }

    [Fact]
    public void ADropOlderThanAMonth_IsNoLongerNews()
    {
        var changes = Public(Change(Eur(82_000), Eur(70_000), Now - ListingPriceHistory.ReductionShownFor - TimeSpan.FromMinutes(1)));

        Assert.Null(ListingPriceHistory.Reduction(changes, Eur(70_000), Now));
    }

    [Fact]
    public void ATokenDrop_EarnsNoBadge()
    {
        var changes = Public(Change(Eur(82_000), Eur(81_900), Now.AddDays(-1)));

        Assert.Null(ListingPriceHistory.Reduction(changes, Eur(81_900), Now));
    }

    [Fact]
    public void AChangeOfCurrency_IsComparedInEuro()
    {
        // 50 000 EUR → 950 000 MDL (47 500 EUR): 5% less.
        var changes = Public(Change(Eur(50_000), Mdl(950_000), Now.AddDays(-1)));

        var reduction = ListingPriceHistory.Reduction(changes, Mdl(950_000), Now)!;

        Assert.Equal((50_000m, "EUR", 5), (reduction.PreviousAmount, reduction.PreviousCurrency, reduction.Percent));
    }

    [Fact]
    public void ChangesBeforePublication_OrOfANeverPublishedListing_AreNotPublic()
    {
        var beforePublication = Change(Eur(90_000), Eur(82_000), Published.AddDays(-1));

        Assert.Empty(ListingPriceHistory.PublicChanges([beforePublication], Published));
        Assert.Empty(ListingPriceHistory.PublicChanges([beforePublication], publishedAt: null));
        Assert.Null(ListingPriceHistory.History([]));
    }

    [Fact]
    public void History_IsOldestFirst_WithSignedPercents()
    {
        var history = ListingPriceHistory.History(Public(
            Change(Eur(79_500), Eur(81_000), Now.AddDays(-1)),
            Change(Eur(82_000), Eur(79_500), Now.AddDays(-3))))!;

        Assert.True(history.StartsAtPublication);
        Assert.Equal([-3, 2], history.Changes.Select(c => c.ChangePercent));
        Assert.Equal(82_000m, history.Changes[0].OldAmount);
    }

    [Fact]
    public void History_KeepsOnlyTheLatestEntries()
    {
        var changes = Enumerable.Range(0, ListingPriceHistory.MaxEntries + 5)
            .Select(i => Change(Eur(100_000 - i), Eur(100_000 - i - 1), Published.AddHours(i + 1)))
            .ToArray();

        var history = ListingPriceHistory.History(Public(changes))!;

        Assert.False(history.StartsAtPublication);
        Assert.Equal(ListingPriceHistory.MaxEntries, history.Changes.Count);
        Assert.Equal(changes[^1].NewAmount, history.Changes[^1].NewAmount);
    }

    [Fact]
    public async Task Loader_PutsTheReductionOnEveryView_AndTheHistoryOnlyOnTheDetailView()
    {
        await using var db = TestDbContextFactory.Create();
        var publisher = ListingTestData.AddIndividualPublisher(db);
        var listing = ListingTestData.AddListing(db, publisher.Id).MoveTo(ListingStatus.Active);
        db.ListingPriceChanges.Add(ListingPriceChange.Between(listing.Id, Eur(600), Eur(550), DateTimeOffset.UtcNow)!);
        await db.SaveChangesAsync();
        var blobs = new FakeBlobStorageService();

        var card = (await ListingDtoLoader.LoadAsync(db, blobs, [listing], null, CancellationToken.None)).Single();
        var detail = await ListingDtoLoader.LoadOneAsync(db, blobs, listing, null, CancellationToken.None, includeContactDetails: true);

        Assert.Equal((600m, 8), (card.PriceReduction!.PreviousAmount, card.PriceReduction.Percent));
        Assert.Null(card.PriceHistory);
        Assert.Equal(-8, Assert.Single(detail.PriceHistory!.Changes).ChangePercent);
    }
}
