using Imova.Domain.Listings;

namespace Imova.UnitTests.Listings;

public class PriceTests
{
    [Fact]
    public void Create_InEur_PriceEurEqualsAmount()
    {
        var price = Price.Create(550m, Currency.EUR, isNegotiable: false, eurRate: 1m);

        Assert.Equal(550m, price.Amount);
        Assert.Equal(Currency.EUR, price.Currency);
        Assert.Equal(550m, price.PriceEur);
        Assert.False(price.IsNegotiable);
    }

    [Theory]
    [InlineData(Currency.MDL, 10_000, 0.051, 510)]
    [InlineData(Currency.USD, 1_000, 0.86, 860)]
    public void Create_InOtherCurrency_ConvertsToEurWithTheGivenRate(Currency currency, decimal amount, decimal rate, decimal expectedEur)
    {
        var price = Price.Create(amount, currency, isNegotiable: true, eurRate: rate);

        Assert.Equal(amount, price.Amount);
        Assert.Equal(expectedEur, price.PriceEur);
        Assert.True(price.IsNegotiable);
    }

    [Fact]
    public void Create_RoundsPriceEurToTwoDecimalsAwayFromZero()
    {
        // 333 * 0.0515 = 17.1495 -> 17.15
        var price = Price.Create(333m, Currency.MDL, false, 0.0515m);

        Assert.Equal(17.15m, price.PriceEur);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveAmount_Throws(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Price.Create(amount, Currency.EUR, false, 1m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    public void Create_WithNonPositiveRate_Throws(decimal rate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Price.Create(100m, Currency.MDL, false, rate));
    }

    [Fact]
    public void Create_InEurWithRateOtherThanOne_Throws()
    {
        Assert.Throws<ArgumentException>(() => Price.Create(100m, Currency.EUR, false, 0.9m));
    }

    [Fact]
    public void Create_WithUndefinedCurrency_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Price.Create(100m, (Currency)99, false, 1m));
    }

    [Fact]
    public void Prices_WithSameValues_AreEqual()
    {
        Assert.Equal(Price.Create(100m, Currency.USD, false, 0.9m), Price.Create(100m, Currency.USD, false, 0.9m));
    }
}
