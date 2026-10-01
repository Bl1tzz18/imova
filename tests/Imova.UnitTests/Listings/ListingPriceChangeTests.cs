using Imova.Domain.Listings;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class ListingPriceChangeTests
{
    private static readonly Guid ListingId = Guid.NewGuid();
    private static readonly DateTimeOffset At = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Between_KeepsBothPricesWithTheirCurrencyAndEurValue()
    {
        var oldPrice = ListingTestData.Eur(60_000m);
        var newPrice = Price.Create(1_100_000m, Currency.MDL, false, eurRate: 0.05m);

        var change = ListingPriceChange.Between(ListingId, oldPrice, newPrice, At);

        Assert.NotNull(change);
        Assert.Equal(ListingId, change.ListingId);
        Assert.Equal((60_000m, Currency.EUR, 60_000m), (change.OldAmount, change.OldCurrency, change.OldPriceEur));
        Assert.Equal((1_100_000m, Currency.MDL, 55_000m), (change.NewAmount, change.NewCurrency, change.NewPriceEur));
        Assert.Equal(At, change.ChangedAt);
    }

    [Fact]
    public void Between_SameAmountInAnotherCurrency_IsAChange()
    {
        var change = ListingPriceChange.Between(
            ListingId, ListingTestData.Eur(500m), Price.Create(500m, Currency.USD, false, eurRate: 0.9m), At);

        Assert.NotNull(change);
    }

    [Fact]
    public void Between_SameAskingPrice_IsNoChange_EvenIfNegotiableOrTheRateMoved()
    {
        Assert.Null(ListingPriceChange.Between(ListingId, ListingTestData.Eur(500m), ListingTestData.Eur(500m, isNegotiable: true), At));
        Assert.Null(ListingPriceChange.Between(
            ListingId,
            Price.Create(10_000m, Currency.MDL, false, eurRate: 0.05m),
            Price.Create(10_000m, Currency.MDL, false, eurRate: 0.051m),
            At));
    }

    [Fact]
    public void Between_NeedsAListing() =>
        Assert.Throws<ArgumentException>(() =>
            ListingPriceChange.Between(Guid.Empty, ListingTestData.Eur(1m), ListingTestData.Eur(2m), At));
}
