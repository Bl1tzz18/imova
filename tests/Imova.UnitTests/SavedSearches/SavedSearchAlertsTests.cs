using Imova.Application.Common;
using Imova.Application.Features.SavedSearches;
using Imova.Application.Features.SavedSearches.Alerts;
using Imova.Application.Features.Telegram;
using Imova.Application.Features.Telegram.Bot;
using Imova.Domain.Listings;
using Imova.Domain.Publishers;
using Imova.Domain.SavedSearches;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.SavedSearches;

public class SavedSearchAlertsTests
{
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly ManualTimeProvider _clock = new(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
    private readonly FakeEmailSender _email = new();
    private readonly FakeTelegramBot _telegram = new();
    private TelegramOptions _telegramOptions = new() { BotToken = "123:test", BotUsername = "ImovaTestBot" };
    private const long ChatId = 777_000_1;
    private readonly SavedSearchUnsubscribeTokens _tokens = new(new EphemeralDataProtectionProvider());
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Publisher _publisher;

    // What the (fake) search finds, by publication time — the real search filters on PublishedAt.
    private readonly List<(Guid Id, DateTimeOffset PublishedAt)> _published = [];

    public SavedSearchAlertsTests()
    {
        ListingTestData.AddUser(_db, _userId);
        _publisher = ListingTestData.AddIndividualPublisher(_db, _userId);
        _db.SaveChanges();
    }

    private Listing Publish(TimeSpan ago)
    {
        var listing = ListingTestData.AddListing(_db, _publisher.Id);
        _db.SaveChanges();
        _published.Add((listing.Id, _clock.Now - ago));
        return listing;
    }

    private FakeListingSearch Search() => new(q =>
    {
        var ids = _published
            .Where(p => (q.PublishedAfter is not { } after || p.PublishedAt > after) && (q.PublishedBefore is not { } before || p.PublishedAt <= before))
            .Select(p => p.Id)
            .ToList();
        return (ids.Take(q.PageSize).ToList(), ids.Count);
    });

    private Task<int> RunAsync()
    {
        var app = new AppOptions { WebBaseUrl = "https://imova.test" };
        var blobs = new FakeBlobStorageService();
        return new SavedSearchAlerts(
                _db, Search(), blobs, _email,
                new SavedSearchTelegramAlert(_telegram, blobs, app, NullLogger<SavedSearchTelegramAlert>.Instance),
                _telegramOptions, app, _tokens, _clock, NullLogger<SavedSearchAlerts>.Instance)
            .RunAsync(CancellationToken.None);
    }

    private void ConnectTelegram(Guid? userId = null)
    {
        _db.TelegramLinks.Add(new TelegramLink { UserId = userId ?? _userId, ChatId = ChatId, LinkedAt = _clock.Now });
        _db.SaveChanges();
    }

    // Everything the fake bot sent, messages and photos, in order of kind.
    private List<string> TelegramTexts() => _telegram.Photos.Select(p => p.Caption).Concat(_telegram.Sent.Select(m => m.Html)).ToList();

    private SavedSearch AddSavedSearch(AlertFrequency frequency, Guid? userId = null, AlertChannels channels = AlertChannels.Email)
    {
        var search = SavedSearch.Create(userId ?? _userId, "Chirii Botanica", "transactionType=Rent", frequency, _clock.Now, channels);
        _db.SavedSearches.Add(search);
        _db.SaveChanges();
        return search;
    }

    [Fact]
    public async Task NewMatches_AreEmailedOnce_WithLinksAndAnUnsubscribeLink()
    {
        var saved = AddSavedSearch(AlertFrequency.Instant);
        _clock.Advance(TimeSpan.FromMinutes(5));
        var listing = Publish(ago: TimeSpan.FromMinutes(2));

        Assert.Equal(1, await RunAsync());

        var email = Assert.Single(_email.Sent);
        Assert.Equal($"{_userId:N}@example.com", email.To);
        Assert.Contains("„Chirii Botanica”", email.Subject);
        Assert.Contains($"https://imova.test/property/{listing.Id}", email.TextBody);
        Assert.Contains($"https://imova.test/saved-searches/{saved.Id}/open", email.TextBody);
        Assert.Contains($"https://imova.test/saved-searches/unsubscribe?id={saved.Id}&token=", email.TextBody);

        // The next run has nothing new to say.
        _clock.Advance(TimeSpan.FromMinutes(5));
        Assert.Equal(0, await RunAsync());
        Assert.Single(_email.Sent);
    }

    [Fact]
    public async Task ListingsPublishedInTheLastMoments_WaitForTheNextRun()
    {
        AddSavedSearch(AlertFrequency.Instant);
        _clock.Advance(TimeSpan.FromMinutes(5));
        Publish(ago: TimeSpan.FromSeconds(5));

        Assert.Equal(0, await RunAsync());

        _clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(1, await RunAsync());
    }

    [Fact]
    public async Task ListingsPublishedBeforeTheSearchWasSaved_AreNotAlerted()
    {
        Publish(ago: TimeSpan.FromDays(1));
        AddSavedSearch(AlertFrequency.Instant);
        _clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(0, await RunAsync());
    }

    [Fact]
    public async Task Daily_SendsAtMostOneEmailADay_CollectingWhatAppearedInBetween()
    {
        AddSavedSearch(AlertFrequency.Daily);
        _clock.Advance(TimeSpan.FromHours(1));
        var first = Publish(ago: TimeSpan.FromMinutes(10));
        Assert.Equal(1, await RunAsync());

        // Two hours later a new match appears — not emailed until a day has passed.
        _clock.Advance(TimeSpan.FromHours(2));
        var second = Publish(ago: TimeSpan.FromMinutes(10));
        Assert.Equal(0, await RunAsync());

        _clock.Advance(TimeSpan.FromHours(22));
        Assert.Equal(1, await RunAsync());

        Assert.Equal(2, _email.Sent.Count);
        Assert.Contains(second.Id.ToString(), _email.Sent[1].TextBody);
        Assert.DoesNotContain(first.Id.ToString(), _email.Sent[1].TextBody);
    }

    [Fact]
    public async Task OffAndUnconfirmedEmails_GetNothing()
    {
        AddSavedSearch(AlertFrequency.Off);
        var unconfirmed = Guid.NewGuid();
        ListingTestData.AddUser(_db, unconfirmed, emailConfirmed: false);
        _db.SaveChanges();
        AddSavedSearch(AlertFrequency.Instant, unconfirmed);
        _clock.Advance(TimeSpan.FromMinutes(5));
        Publish(ago: TimeSpan.FromMinutes(2));

        Assert.Equal(0, await RunAsync());
        Assert.Empty(_email.Sent);
    }

    [Fact]
    public async Task AFailedEmail_IsRetriedOnTheNextRun()
    {
        AddSavedSearch(AlertFrequency.Instant);
        _clock.Advance(TimeSpan.FromMinutes(5));
        var listing = Publish(ago: TimeSpan.FromMinutes(2));
        _email.Fail = true;

        Assert.Equal(0, await RunAsync());

        _email.Fail = false;
        _clock.Advance(TimeSpan.FromMinutes(5));
        Assert.Equal(1, await RunAsync());
        Assert.Contains(listing.Id.ToString(), Assert.Single(_email.Sent).TextBody);
    }

    [Fact]
    public async Task TheUnsubscribeLinksToken_IsValidForThatSearch()
    {
        var saved = AddSavedSearch(AlertFrequency.Instant);
        _clock.Advance(TimeSpan.FromMinutes(5));
        Publish(ago: TimeSpan.FromMinutes(2));

        await RunAsync();

        var body = Assert.Single(_email.Sent).TextBody;
        var token = Uri.UnescapeDataString(body[(body.IndexOf("token=", StringComparison.Ordinal) + "token=".Length)..].Trim());
        Assert.True(_tokens.IsValid(saved.Id, token));
    }

    [Theory]
    [InlineData(AlertChannels.Email, true, true, true, false)]
    [InlineData(AlertChannels.Telegram, true, true, false, true)]
    [InlineData(AlertChannels.Both, true, true, true, true)]
    [InlineData(AlertChannels.Both, false, true, false, true)]
    [InlineData(AlertChannels.Telegram, true, false, true, false)]
    [InlineData(AlertChannels.Telegram, false, false, false, false)]
    [InlineData(AlertChannels.Email, false, true, false, false)]
    public void Targets_FollowTheChannels_AndFallBackToEmailWhenTelegramIsGone(
        AlertChannels channels, bool emailUsable, bool telegramUsable, bool email, bool telegram) =>
        Assert.Equal((email, telegram), SavedSearchAlerts.Targets(channels, emailUsable, telegramUsable));

    [Fact]
    public async Task TelegramOnly_SendsEachListingToTheChat_WithAViewLinkAndAStopButton_NoEmail()
    {
        ConnectTelegram();
        var saved = AddSavedSearch(AlertFrequency.Instant, channels: AlertChannels.Telegram);
        _clock.Advance(TimeSpan.FromMinutes(5));
        var listing = Publish(ago: TimeSpan.FromMinutes(2));

        Assert.Equal(1, await RunAsync());

        Assert.Empty(_email.Sent);
        var message = Assert.Single(_telegram.Sent);
        Assert.Equal(ChatId, message.ChatId);
        Assert.Contains("🔔 <i>Alertă pentru „Chirii Botanica”</i>", message.Html);
        Assert.Equal($"https://imova.test/property/{listing.Id}", message.Buttons[0].Url);
        Assert.Equal(TelegramBotUpdates.StopAlertData(saved.Id), message.Buttons[1].CallbackData);

        _clock.Advance(TimeSpan.FromMinutes(5));
        Assert.Equal(0, await RunAsync());
        Assert.Single(_telegram.Sent);
    }

    [Fact]
    public async Task Both_SendsTheEmailAndTheTelegramAlert()
    {
        ConnectTelegram();
        AddSavedSearch(AlertFrequency.Instant, channels: AlertChannels.Both);
        _clock.Advance(TimeSpan.FromMinutes(5));
        Publish(ago: TimeSpan.FromMinutes(2));

        Assert.Equal(2, await RunAsync());

        Assert.Single(_email.Sent);
        Assert.Single(_telegram.Sent);
    }

    [Fact]
    public async Task TelegramWithoutAConnectedChat_FallsBackToEmail()
    {
        AddSavedSearch(AlertFrequency.Instant, channels: AlertChannels.Telegram);
        _clock.Advance(TimeSpan.FromMinutes(5));
        Publish(ago: TimeSpan.FromMinutes(2));

        Assert.Equal(1, await RunAsync());

        Assert.Single(_email.Sent);
        Assert.Empty(TelegramTexts());
    }

    [Fact]
    public async Task AWorkerWithoutABot_SendsEmailInstead()
    {
        _telegramOptions = new TelegramOptions();
        ConnectTelegram();
        AddSavedSearch(AlertFrequency.Instant, channels: AlertChannels.Telegram);
        _clock.Advance(TimeSpan.FromMinutes(5));
        Publish(ago: TimeSpan.FromMinutes(2));

        Assert.Equal(1, await RunAsync());

        Assert.Single(_email.Sent);
        Assert.Empty(TelegramTexts());
    }

    [Fact]
    public async Task AnUnconfirmedEmail_StillGetsTelegramAlerts()
    {
        var unconfirmed = Guid.NewGuid();
        ListingTestData.AddUser(_db, unconfirmed, emailConfirmed: false);
        _db.SaveChanges();
        ConnectTelegram(unconfirmed);
        AddSavedSearch(AlertFrequency.Instant, unconfirmed, AlertChannels.Both);
        _clock.Advance(TimeSpan.FromMinutes(5));
        Publish(ago: TimeSpan.FromMinutes(2));

        Assert.Equal(1, await RunAsync());

        Assert.Empty(_email.Sent);
        Assert.Single(_telegram.Sent);
    }

    [Fact]
    public async Task AChatThatBlockedTheBot_IsForgotten_AndTheNextRunEmailsTheSameListings()
    {
        ConnectTelegram();
        AddSavedSearch(AlertFrequency.Instant, channels: AlertChannels.Telegram);
        _clock.Advance(TimeSpan.FromMinutes(5));
        Publish(ago: TimeSpan.FromMinutes(2));
        _telegram.UnavailableChats.Add(ChatId);

        Assert.Equal(0, await RunAsync());
        Assert.Empty(_db.TelegramLinks);

        _clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(1, await RunAsync());
        Assert.Single(_email.Sent);
    }

    [Fact]
    public async Task WhenEveryChannelFails_TheSameListingsAreRetried()
    {
        ConnectTelegram();
        AddSavedSearch(AlertFrequency.Instant, channels: AlertChannels.Both);
        _clock.Advance(TimeSpan.FromMinutes(5));
        Publish(ago: TimeSpan.FromMinutes(2));
        _telegram.FailAfter = 0;
        _email.Fail = true;

        Assert.Equal(0, await RunAsync());

        _telegram.FailAfter = null;
        _email.Fail = false;
        _clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(2, await RunAsync());
    }

    [Fact]
    public async Task WhenOneChannelWorks_TheListingsAreNotSentAgain()
    {
        ConnectTelegram();
        AddSavedSearch(AlertFrequency.Instant, channels: AlertChannels.Both);
        _clock.Advance(TimeSpan.FromMinutes(5));
        Publish(ago: TimeSpan.FromMinutes(2));
        _email.Fail = true;

        Assert.Equal(1, await RunAsync());

        _email.Fail = false;
        _clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(0, await RunAsync());
        Assert.Single(_telegram.Sent);
    }

    [Fact]
    public async Task ManyListings_FiveMessages_ThenOneWithTheRest()
    {
        ConnectTelegram();
        var saved = AddSavedSearch(AlertFrequency.Instant, channels: AlertChannels.Telegram);
        _clock.Advance(TimeSpan.FromMinutes(5));
        for (var i = 0; i < 8; i++)
        {
            Publish(ago: TimeSpan.FromMinutes(2));
        }

        await RunAsync();

        Assert.Equal(6, _telegram.Sent.Count);
        var more = _telegram.Sent[^1];
        Assert.Equal("Și încă 3 anunțuri noi pentru „Chirii Botanica”.", more.Html);
        Assert.Equal($"https://imova.test/saved-searches/{saved.Id}/open", Assert.Single(more.Buttons).Url);
    }

    [Fact]
    public async Task ARateLimitAfterTheFirstMessage_DropsTheRest_WithoutResending()
    {
        ConnectTelegram();
        AddSavedSearch(AlertFrequency.Instant, channels: AlertChannels.Telegram);
        _clock.Advance(TimeSpan.FromMinutes(5));
        Publish(ago: TimeSpan.FromMinutes(2));
        Publish(ago: TimeSpan.FromMinutes(2));
        _telegram.FailAfter = 1;

        Assert.Equal(1, await RunAsync());

        _telegram.FailAfter = null;
        _clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(0, await RunAsync());
        Assert.Single(_telegram.Sent);
    }
}
