using Imova.Application.Features.Favorites.Alerts;

namespace Imova.UnitTests.Favorites;

public class FavoriteAlertEmailsTests
{
    private static FavoriteAlertEmails.PriceChange Change(decimal oldAmount, decimal newAmount, string oldCurrency = "EUR", string newCurrency = "EUR") =>
        new(Guid.NewGuid(), "Casă <b>mare</b>", "Sale", oldAmount, oldCurrency, newAmount, newCurrency);

    [Theory]
    [InlineData(100_000, 95_000, "−5%")]
    [InlineData(100_000, 103_000, "+3%")]
    [InlineData(100_000, 100_200, null)]
    public void ChangePercent_IsSignedAndRounded(int oldAmount, int newAmount, string? expected)
    {
        Assert.Equal(expected, FavoriteAlertEmails.ChangePercent(Change(oldAmount, newAmount)));
    }

    [Fact]
    public void ChangePercent_NotShownAcrossCurrencies()
    {
        Assert.Null(FavoriteAlertEmails.ChangePercent(Change(100_000, 1_900_000, "EUR", "MDL")));
    }

    [Fact]
    public void PriceChanged_EncodesTheTitle_AndShowsBothPrices()
    {
        var email = FavoriteAlertEmails.PriceChanged(
            "a@example.com", Change(100_000, 95_000), "https://imova.test/property/1", "https://imova.test/u", "https://imova.test/s");

        Assert.DoesNotContain("<b>mare</b>", email.HtmlBody);
        Assert.Contains("Casă &lt;b&gt;mare&lt;/b&gt;", email.HtmlBody);
        Assert.Contains("100 000 EUR", email.HtmlBody);
        Assert.Contains("95 000 EUR", email.HtmlBody);
        Assert.Contains("Casă <b>mare</b>", email.TextBody);
        Assert.Contains("https://imova.test/u", email.HtmlBody);
    }

    [Fact]
    public void RentPrices_ArePerMonth()
    {
        var email = FavoriteAlertEmails.PriceChanged(
            "a@example.com",
            new FavoriteAlertEmails.PriceChange(Guid.NewGuid(), "Garsonieră", "Rent", 400, "EUR", 350, "EUR"),
            "https://imova.test/property/1", "https://imova.test/u", "https://imova.test/s");

        Assert.Contains("Preț vechi: 400 EUR / lună", email.TextBody);
        Assert.Contains("Preț nou: 350 EUR / lună (−13%)", email.TextBody);
    }
}
