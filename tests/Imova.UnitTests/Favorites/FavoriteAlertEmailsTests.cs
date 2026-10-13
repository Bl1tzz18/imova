using Imova.Application.Common;
using Imova.Application.Features.Favorites.Alerts;
using Imova.Contracts.Listings;

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
            "a@example.com", Change(100_000, 95_000), "https://imova.test/property/1", "https://imova.test/u", "https://imova.test/s",
            Languages.Romanian);

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
            "https://imova.test/property/1", "https://imova.test/u", "https://imova.test/s",
            Languages.Romanian);

        Assert.Contains("Preț vechi: 400 EUR / lună", email.TextBody);
        Assert.Contains("Preț nou: 350 EUR / lună (−13%)", email.TextBody);
    }

    [Theory]
    [InlineData("en", "The price has dropped: Garsonieră — IMOVA", "Old price: 400 EUR / month", "Room · Rent · 30 m²")]
    [InlineData("ru", "Цена снизилась: Garsonieră — IMOVA", "Старая цена: 400 EUR / мес.", "Комната · Аренда · 30 m²")]
    public void PriceChanged_IsWrittenInTheUsersLanguage(string language, string subject, string oldPrice, string facts)
    {
        var email = FavoriteAlertEmails.PriceChanged(
            "a@example.com",
            new FavoriteAlertEmails.PriceChange(Guid.NewGuid(), "Garsonieră", "Rent", 400, "EUR", 350, "EUR", "Room", 30),
            "https://imova.test/property/1", "https://imova.test/u", "https://imova.test/s",
            language);

        Assert.Equal(subject, email.Subject);
        Assert.Contains(oldPrice, email.TextBody);
        Assert.Contains(facts, email.TextBody);
        Assert.Contains(facts, email.HtmlBody);
        Assert.DoesNotContain("Preț", email.HtmlBody);
    }

    [Theory]
    [InlineData("ro", "Nu mai este disponibil: Casă — IMOVA", "Proprietatea a fost vândută.", ">Vândut<")]
    [InlineData("en", "No longer available: Casă — IMOVA", "The property has been sold.", ">Sold<")]
    [InlineData("ru", "Больше недоступно: Casă — IMOVA", "Недвижимость продана.", ">Продано<")]
    public void Ended_SaysWhy_InTheUsersLanguage(string language, string subject, string reason, string badge)
    {
        var ended = new EndedListingDto(
            Guid.NewGuid(), "Sold", "Sale", "House", "Casă", new PriceDto(90_000, "EUR", 90_000, false), 120,
            Guid.NewGuid(), "Chișinău", null, "Botanica", DateTimeOffset.UtcNow);

        var email = FavoriteAlertEmails.Ended(
            "a@example.com", ended, "https://imova.test/property/1", "https://imova.test/property/1#s", "https://imova.test/search",
            "https://imova.test/u", "https://imova.test/s", language);

        Assert.Equal(subject, email.Subject);
        Assert.Contains(reason, email.TextBody);
        Assert.Contains(badge, email.HtmlBody);
    }
}
