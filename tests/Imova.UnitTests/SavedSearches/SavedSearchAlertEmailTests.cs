using Imova.Application.Common;
using Imova.Application.Features.SavedSearches.Alerts;

namespace Imova.UnitTests.SavedSearches;

public class SavedSearchAlertEmailTests
{
    private const string Ro = Languages.Romanian;

    private static AlertListing Listing(
        string title = "Garaj subteran, Botanica",
        string transaction = "Sale",
        decimal amount = 9000,
        string? photo = "https://storage.test/listing-images/g.jpg",
        string? location = "Botanica, Chișinău") =>
        new(Guid.NewGuid(), title, amount, "EUR", transaction, "Garage", 18, location, photo, "https://imova.test/property/1");

    private static readonly string Open = "https://imova.test/saved-searches/7/open";
    private static readonly string Unsubscribe = "https://imova.test/saved-searches/unsubscribe?id=7&token=abc";

    [Fact]
    public void Html_ShowsACardPerListing_WithItsPhotoPriceAndFacts()
    {
        var email = SavedSearchAlertEmail.Build("ana@example.com", "Garaje", [Listing()], total: 1, Open, Unsubscribe, Ro);

        Assert.Equal("Un anunț nou pentru „Garaje” — IMOVA", email.Subject);
        Assert.NotNull(email.HtmlBody);
        Assert.Contains("<img src=\"https://storage.test/listing-images/g.jpg\"", email.HtmlBody);
        Assert.Contains("9 000 EUR", email.HtmlBody);
        Assert.Contains("Garaj · Vânzare · 18 m²", email.HtmlBody);
        Assert.Contains("Botanica, Chișinău", email.HtmlBody);
        Assert.Contains("href=\"https://imova.test/property/1\"", email.HtmlBody);
        Assert.Contains($"href=\"{Open}\"", email.HtmlBody);
        Assert.Contains("href=\"https://imova.test/saved-searches/unsubscribe?id=7&amp;token=abc\"", email.HtmlBody);
    }

    [Fact]
    public void ARental_ShowsItsPricePerMonth()
    {
        Assert.Equal("450 EUR / lună", SavedSearchAlertEmail.Price(Listing(transaction: "Rent", amount: 450), Ro));
        Assert.Equal("Garaj · Chirie · 18 m²", SavedSearchAlertEmail.Facts(Listing(transaction: "Rent"), Ro));
    }

    [Fact]
    public void AListingWithoutAPhoto_GetsAPlaceholder()
    {
        var email = SavedSearchAlertEmail.Build("ana@example.com", "Garaje", [Listing(photo: null)], total: 1, Open, Unsubscribe, Ro);

        Assert.DoesNotContain("<img", email.HtmlBody);
        Assert.Contains("fără foto", email.HtmlBody);
    }

    [Fact]
    public void UserWrittenText_IsHtmlEncoded()
    {
        var email = SavedSearchAlertEmail.Build(
            "ana@example.com", "Mele <b>", [Listing(title: "<script>alert(1)</script> & co")], total: 1, Open, Unsubscribe, Ro);

        Assert.DoesNotContain("<script>", email.HtmlBody);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt; &amp; co", email.HtmlBody);
        Assert.Contains("Mele &lt;b&gt;", email.HtmlBody);
    }

    [Fact]
    public void MoreMatchesThanCards_SaysHowManyMore_InBothVersions()
    {
        var email = SavedSearchAlertEmail.Build("ana@example.com", "Garaje", [Listing(), Listing()], total: 12, Open, Unsubscribe, Ro);

        Assert.Equal("12 anunțuri noi pentru „Garaje” — IMOVA", email.Subject);
        Assert.Contains("…și încă 10.", email.HtmlBody);
        Assert.Contains("…și încă 10.", email.TextBody);
    }

    [Fact]
    public void TheTextVersion_HasTheSameListingsAndLinks()
    {
        var email = SavedSearchAlertEmail.Build("ana@example.com", "Garaje", [Listing()], total: 1, Open, Unsubscribe, Ro);

        Assert.Contains("• Garaj subteran, Botanica — 9 000 EUR", email.TextBody);
        Assert.Contains("Garaj · Vânzare · 18 m² · Botanica, Chișinău", email.TextBody);
        Assert.Contains("https://imova.test/property/1", email.TextBody);
        Assert.Contains(Open, email.TextBody);
        Assert.Contains(Unsubscribe, email.TextBody);
    }

    [Theory]
    [InlineData("en", "12 new listings for “Garaje” — IMOVA", "Garage · Sale · 18 m²", "…and 10 more.", "no photo")]
    [InlineData("ru", "12 новых объявлений по запросу «Garaje» — IMOVA", "Гараж · Продажа · 18 m²", "…и ещё 10.", "без фото")]
    public void TheEmail_IsWrittenInTheUsersLanguage(string language, string subject, string facts, string more, string noPhoto)
    {
        var email = SavedSearchAlertEmail.Build(
            "ana@example.com", "Garaje", [Listing(photo: null), Listing()], total: 12, Open, Unsubscribe, language);

        Assert.Equal(subject, email.Subject);
        Assert.Contains(facts, email.HtmlBody);
        Assert.Contains(facts, email.TextBody);
        Assert.Contains(more, email.TextBody);
        Assert.Contains(noPhoto, email.HtmlBody);
        Assert.Contains($"<html lang=\"{language}\">", email.HtmlBody);
    }

    [Theory]
    [InlineData("ro", 1, "Un anunț nou")]
    [InlineData("ro", 19, "19 anunțuri noi")]
    [InlineData("ro", 20, "20 de anunțuri noi")]
    [InlineData("ro", 101, "101 anunțuri noi")]
    [InlineData("ro", 120, "120 de anunțuri noi")]
    [InlineData("en", 1, "1 new listing")]
    [InlineData("en", 2, "2 new listings")]
    [InlineData("ru", 1, "1 новое объявление")]
    [InlineData("ru", 3, "3 новых объявления")]
    [InlineData("ru", 5, "5 новых объявлений")]
    [InlineData("ru", 11, "11 новых объявлений")]
    [InlineData("ru", 14, "14 новых объявлений")]
    [InlineData("ru", 21, "21 новое объявление")]
    [InlineData("ru", 22, "22 новых объявления")]
    public void TheHeadline_CountsInEachLanguagesGrammar(string language, int total, string expected)
    {
        Assert.Equal(expected, SavedSearchAlertEmail.Headline(total, language));
    }
}
