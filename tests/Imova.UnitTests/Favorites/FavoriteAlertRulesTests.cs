using Imova.Domain.Favorites;
using Imova.Domain.Listings;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Favorites;

// The debounce/dedup rules a favorite keeps for its alert emails (see Favorite).
public class FavoriteAlertRulesTests
{
    private static readonly DateTimeOffset Saved = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

    private static Favorite SavedAt(decimal price) =>
        Favorite.Create(Guid.NewGuid(), Guid.NewGuid(), ListingTestData.Eur(price), now: Saved);

    [Fact]
    public void Create_RemembersThePriceTheUserSaw()
    {
        var favorite = SavedAt(550m);

        Assert.Equal(550m, favorite.KnownPriceAmount);
        Assert.Equal(Currency.EUR, favorite.KnownPriceCurrency);
        Assert.Equal(Saved, favorite.CreatedAt);
        Assert.False(favorite.AlertsStopped);
    }

    [Fact]
    public void SamePrice_IsNotDue()
    {
        Assert.False(SavedAt(550m).IsPriceAlertDue(ListingTestData.Eur(550m), Saved.AddDays(3)));
    }

    [Fact]
    public void NegotiableFlagAlone_IsNotAPriceChange()
    {
        Assert.False(SavedAt(550m).IsPriceAlertDue(ListingTestData.Eur(550m, isNegotiable: true), Saved.AddDays(3)));
    }

    [Fact]
    public void CurrencyChange_IsAPriceChange()
    {
        var mdl = Price.Create(550m, Currency.MDL, isNegotiable: false, eurRate: 0.051m);

        Assert.True(SavedAt(550m).IsPriceAlertDue(mdl, Saved.AddMinutes(1)));
    }

    [Fact]
    public void FirstChange_IsDueRightAway()
    {
        Assert.True(SavedAt(550m).IsPriceAlertDue(ListingTestData.Eur(500m), Saved.AddMinutes(1)));
    }

    [Fact]
    public void AfterAnEmail_TheNextChangeWaitsADay()
    {
        var favorite = SavedAt(550m);
        var firstEmail = Saved.AddHours(1);
        favorite.RecordPrice(ListingTestData.Eur(500m), firstEmail, emailSent: true);

        Assert.False(favorite.IsPriceAlertDue(ListingTestData.Eur(480m), firstEmail.AddHours(23).AddMinutes(59)));
        Assert.True(favorite.IsPriceAlertDue(ListingTestData.Eur(480m), firstEmail.Add(Favorite.PriceAlertInterval)));
    }

    [Fact]
    public void PriceBackToTheKnownOne_IsNotDue()
    {
        var favorite = SavedAt(550m);
        favorite.RecordPrice(ListingTestData.Eur(500m), Saved, emailSent: true);

        Assert.False(favorite.IsPriceAlertDue(ListingTestData.Eur(500m), Saved.AddDays(2)));
    }

    [Fact]
    public void SilentCatchUp_DoesNotStartTheWindow()
    {
        var favorite = SavedAt(550m);
        favorite.RecordPrice(ListingTestData.Eur(500m), Saved, emailSent: false);

        Assert.Equal(500m, favorite.KnownPriceAmount);
        Assert.Null(favorite.PriceAlertSentAt);
        Assert.True(favorite.IsPriceAlertDue(ListingTestData.Eur(450m), Saved.AddMinutes(1)));
    }

    [Fact]
    public void AfterTheEndedAlert_NoPriceAlertEver()
    {
        var favorite = SavedAt(550m);
        favorite.RecordEnded(Saved.AddHours(1));

        Assert.True(favorite.AlertsStopped);
        Assert.False(favorite.IsPriceAlertDue(ListingTestData.Eur(300m), Saved.AddDays(30)));
    }

    [Fact]
    public void RecordEnded_KeepsTheFirstTime()
    {
        var favorite = SavedAt(550m);
        favorite.RecordEnded(Saved.AddHours(1));
        favorite.RecordEnded(Saved.AddHours(5));

        Assert.Equal(Saved.AddHours(1), favorite.EndedAlertSentAt);
    }

    [Fact]
    public void SavingAnEndedListing_HasNothingToTell()
    {
        var favorite = Favorite.Create(Guid.NewGuid(), Guid.NewGuid(), ListingTestData.Eur(550m), listingHasEnded: true, now: Saved);

        Assert.True(favorite.AlertsStopped);
        Assert.Equal(Saved, favorite.EndedAlertSentAt);
    }

    [Fact]
    public void WithoutAKnownPrice_NothingIsDue()
    {
        var favorite = Favorite.Create(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(favorite.KnowsPrice);
        Assert.False(favorite.IsPriceAlertDue(ListingTestData.Eur(500m), DateTimeOffset.UtcNow));
    }
}
