using Imova.Application.Common;
using Imova.Application.Features.SavedSearches.Alerts;
using Imova.Application.Features.Telegram.Bot;
using Imova.Domain.SavedSearches;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.SavedSearches;

// One saved search's listings as Telegram messages.
public class SavedSearchTelegramAlertTests
{
    private const long ChatId = 42;
    private readonly FakeTelegramBot _bot = new();
    private readonly SavedSearch _search = SavedSearch.Create(Guid.NewGuid(), "Case <Ialoveni>", "transactionType=Sale", AlertFrequency.Instant, DateTimeOffset.UtcNow);

    private SavedSearchTelegramAlert Alert() => new(
        _bot, new FakeBlobStorageService(), new AppOptions { WebBaseUrl = "https://imova.md" }, NullLogger<SavedSearchTelegramAlert>.Instance);

    private static AlertListing Listing(string? photoUrl = "https://blob.test/a_800.jpg", string title = "Casă cu 3 camere") =>
        new(Guid.NewGuid(), title, 85000m, "EUR", "Sale", "House", 120m, "Ialoveni, Ialoveni", photoUrl, "https://imova.md/property/x");

    [Fact]
    public async Task AListingWithAPhoto_IsSentAsAPhotoWithItsCaption()
    {
        Assert.True(await Alert().SendAsync(ChatId, "ro", _search, [Listing()], 1, CancellationToken.None));

        var photo = Assert.Single(_bot.Photos);
        Assert.Equal("https://blob.test/a_800.jpg", photo.Photo.Url);
        Assert.Equal(
            "<b>85 000 €</b> · Vânzare\nCasă cu 3 camere\n\n🏠 Casă · 120 m²\n📍 Ialoveni, Ialoveni\n\n🔔 <i>Alertă pentru „Case &lt;Ialoveni&gt;”</i>",
            photo.Caption);
        Assert.Equal(["Vezi anunțul", "Oprește alerta"], photo.Buttons.Select(b => b.Text));
        Assert.Empty(_bot.Sent);
    }

    [Fact]
    public async Task APhotoTelegramRefuses_StillSendsTheListingAsText()
    {
        _bot.RefusePhotos = true;

        Assert.True(await Alert().SendAsync(ChatId, "en", _search, [Listing()], 1, CancellationToken.None));

        Assert.Contains("Alert for “Case &lt;Ialoveni&gt;”", Assert.Single(_bot.Sent).Html);
    }

    [Fact]
    public async Task NoPhoto_IsATextMessage()
    {
        await Alert().SendAsync(ChatId, "ru", _search, [Listing(photoUrl: null)], 1, CancellationToken.None);

        Assert.Contains("Уведомление по поиску", Assert.Single(_bot.Sent).Html);
        Assert.Empty(_bot.Photos);
    }

    [Fact]
    public async Task NothingSent_Throws_SoTheJobRetries()
    {
        _bot.FailAfter = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Alert().SendAsync(ChatId, "ro", _search, [Listing(photoUrl: null)], 1, CancellationToken.None));
    }

    [Fact]
    public void Caption_ForARental_ShowsThePerMonthPrice_InLei()
    {
        var rental = Listing() with { Amount = 9000m, Currency = "MDL", TransactionType = "Rent", Location = null, AreaM2 = 0 };

        Assert.Equal(
            "<b>9 000 lei / lună</b> · Chirie\nCasă cu 3 camere\n\n🏠 Casă\n\n🔔 <i>Alertă pentru „S”</i>",
            SavedSearchTelegramAlert.Caption("ro", "S", rental));
    }

    [Fact]
    public void Caption_ShortensAVeryLongTitle_AndStaysWithinTelegramsLimit()
    {
        var caption = SavedSearchTelegramAlert.Caption("ro", new string('x', 100), Listing(title: new string('a', 900)));

        Assert.Contains(new string('a', 199) + "…", caption);
        Assert.True(caption.Length <= 1024, $"{caption.Length}");
    }

    [Theory]
    [InlineData("ro", 1, "Și încă un anunț nou pentru „S”.")]
    [InlineData("ro", 3, "Și încă 3 anunțuri noi pentru „S”.")]
    [InlineData("ro", 20, "Și încă 20 de anunțuri noi pentru „S”.")]
    [InlineData("en", 1, "And 1 more new listing for “S”.")]
    [InlineData("en", 4, "And 4 more new listings for “S”.")]
    [InlineData("ru", 1, "И ещё 1 новое объявление по поиску «S».")]
    [InlineData("ru", 3, "И ещё 3 новых объявления по поиску «S».")]
    [InlineData("ru", 5, "И ещё 5 новых объявлений по поиску «S».")]
    [InlineData("ru", 11, "И ещё 11 новых объявлений по поиску «S».")]
    [InlineData("ru", 22, "И ещё 22 новых объявления по поиску «S».")]
    public void MoreListings_UsesEachLanguagesPlurals(string language, int count, string expected) =>
        Assert.Equal(expected, TelegramBotTexts.MoreListings(language, count, "S"));
}
