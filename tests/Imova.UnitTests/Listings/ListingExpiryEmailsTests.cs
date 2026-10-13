using Imova.Application.Common;
using Imova.Application.Features.Listings.Expiry;

namespace Imova.UnitTests.Listings;

public class ListingExpiryEmailsTests
{
    private const string MyListings = "https://imova.test/my-listings";

    [Fact]
    public void Reminder_NamesTheListingAndDate_AndLinksToRenew()
    {
        // 22:30 UTC on 11 March is already 12 March in Chișinău (UTC+2).
        var expiresAt = new DateTimeOffset(2027, 3, 11, 22, 30, 0, TimeSpan.Zero);

        var email = ListingExpiryEmails.Reminder("ana@example.com", "Casă <nouă> & grădină", expiresAt, MyListings, Languages.Romanian);

        Assert.Equal("Anunțul tău expiră pe 12.03.2027 — IMOVA", email.Subject);
        Assert.Contains("„Casă &lt;nouă&gt; &amp; grădină”", email.HtmlBody);
        Assert.Contains(">Prelungește anunțul</a>", email.HtmlBody);
        Assert.Contains($"href=\"{MyListings}\"", email.HtmlBody);
        Assert.Contains("6 luni", email.HtmlBody);
        Assert.Contains("„Casă <nouă> & grădină”", email.TextBody);
        Assert.Contains(MyListings, email.TextBody);
    }

    [Fact]
    public void Expired_SaysItCanBeReactivated()
    {
        var email = ListingExpiryEmails.Expired("ana@example.com", "Garaj Botanica", MyListings, Languages.Romanian);

        Assert.Equal("Anunțul tău a expirat — IMOVA", email.Subject);
        Assert.Contains(">Reactivează anunțul</a>", email.HtmlBody);
        Assert.Contains("fără o nouă verificare", email.TextBody);
        Assert.Contains(MyListings, email.TextBody);
    }

    [Theory]
    [InlineData("en", "Your listing expires on 12.03.2027 — IMOVA", ">Renew the listing</a>", "6 months")]
    [InlineData("ru", "Срок вашего объявления истекает 12.03.2027 — IMOVA", ">Продлить объявление</a>", "6 месяцев")]
    public void Reminder_IsWrittenInTheOwnersLanguage(string language, string subject, string button, string period)
    {
        var expiresAt = new DateTimeOffset(2027, 3, 11, 22, 30, 0, TimeSpan.Zero);

        var email = ListingExpiryEmails.Reminder("ana@example.com", "Garaj", expiresAt, MyListings, language);

        Assert.Equal(subject, email.Subject);
        Assert.Contains(button, email.HtmlBody);
        Assert.Contains(period, email.TextBody);
        Assert.Contains(MyListings, email.TextBody);
    }

    [Theory]
    [InlineData("en", "Your listing has expired — IMOVA", ">Reactivate the listing</a>")]
    [InlineData("ru", "Срок вашего объявления истёк — IMOVA", ">Активировать объявление</a>")]
    public void Expired_IsWrittenInTheOwnersLanguage(string language, string subject, string button)
    {
        var email = ListingExpiryEmails.Expired("ana@example.com", "Garaj", MyListings, language);

        Assert.Equal(subject, email.Subject);
        Assert.Contains(button, email.HtmlBody);
    }
}
