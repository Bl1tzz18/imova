using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Telegram;
using Imova.Application.Features.Telegram.Bot;
using Imova.Domain.SavedSearches;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Telegram;

// The bot's side of connecting: pressing Start on a "Conectează Telegram" link, /stop, /cautari.
public class TelegramBotUpdatesTests
{
    private const long ChatId = 555_000_111;
    private static readonly DateTimeOffset Now = new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);

    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly ImovaDbContext _dbContext;
    private readonly FakeTelegramBot _bot = new();
    private readonly ManualTimeProvider _time = new(Now);
    private readonly TelegramBotUpdates _updates;

    public TelegramBotUpdatesTests()
    {
        _dbContext = TestDbContextFactory.Create(_databaseName);
        _updates = new TelegramBotUpdates(_dbContext, _bot, new AppOptions { WebBaseUrl = "https://imova.md" }, _time);
    }

    private static TelegramIncomingMessage Message(string text, long chatId = ChatId, bool isPrivate = true, string? language = "ro") =>
        new(chatId, isPrivate, text, "ion_p", "Ion", language);

    private async Task<string> AddCodeAsync(Guid userId, TimeSpan? expiresIn = null)
    {
        var code = TelegramLinkCodes.NewCode();
        _dbContext.TelegramLinkCodes.Add(new TelegramLinkCode
        {
            Id = Guid.NewGuid(), UserId = userId, CodeHash = TelegramLinkCodes.Hash(code), CreatedAt = Now,
            ExpiresAt = Now + (expiresIn ?? TelegramLinkCodes.Lifetime),
        });
        await _dbContext.SaveChangesAsync();
        return code;
    }

    private Guid AddUser(string language = "ro")
    {
        var user = ListingTestData.AddUser(_dbContext, Guid.NewGuid());
        user.PreferredLanguage = language;
        _dbContext.SaveChanges();
        return user.Id;
    }

    private async Task<List<TelegramLink>> LinksAsync()
    {
        await using var fresh = TestDbContextFactory.Create(_databaseName);
        return await fresh.TelegramLinks.AsNoTracking().ToListAsync();
    }

    [Fact]
    public async Task StartWithAValidCode_ConnectsTheChat_UsesTheCodeOnce_AndAnswersInTheAccountsLanguage()
    {
        var userId = AddUser("en");
        var code = await AddCodeAsync(userId);

        await _updates.HandleAsync(Message($"/start {code}", language: "ru"), CancellationToken.None);

        var link = Assert.Single(await LinksAsync());
        Assert.Equal((userId, ChatId, "ion_p", "Ion", Now), (link.UserId, link.ChatId, link.TelegramUsername, link.TelegramFirstName, link.LinkedAt));
        Assert.Empty(_dbContext.TelegramLinkCodes);
        var reply = Assert.Single(_bot.Sent);
        Assert.StartsWith("Done! Telegram is now connected", reply.Html);
        Assert.Contains($"{userId:N}@example.com", reply.Html);
    }

    [Fact]
    public async Task TheSameCodeTwice_SecondTimeSaysTheLinkExpired()
    {
        var code = await AddCodeAsync(AddUser());

        await _updates.HandleAsync(Message($"/start {code}"), CancellationToken.None);
        await _updates.HandleAsync(Message($"/start {code}", chatId: 999), CancellationToken.None);

        Assert.Equal(ChatId, Assert.Single(await LinksAsync()).ChatId);
        Assert.StartsWith("Linkul de conectare a expirat", _bot.Sent[1].Html);
    }

    [Fact]
    public async Task AnExpiredCode_ConnectsNothing_AndIsDeleted()
    {
        var code = await AddCodeAsync(AddUser(), expiresIn: TimeSpan.FromMinutes(15));
        _time.Advance(TimeSpan.FromMinutes(15));

        await _updates.HandleAsync(Message($"/start {code}", language: "ru"), CancellationToken.None);

        Assert.Empty(await LinksAsync());
        Assert.Empty(_dbContext.TelegramLinkCodes);
        var reply = Assert.Single(_bot.Sent);
        Assert.StartsWith("Ссылка для подключения устарела", reply.Html);
        // In the chat's language, like the text: the Russian site.
        Assert.Equal("https://imova.md/ru/account?tab=notifications", Assert.Single(reply.Buttons).Url);
    }

    [Theory]
    [InlineData("/start not-a-code")]
    [InlineData("/start aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task AnUnknownCode_SaysTheLinkExpired(string text)
    {
        await _updates.HandleAsync(Message(text), CancellationToken.None);

        Assert.Empty(await LinksAsync());
        Assert.StartsWith("Linkul de conectare a expirat", Assert.Single(_bot.Sent).Html);
    }

    [Fact]
    public async Task ConnectingFromANewChat_MovesTheAccountThere()
    {
        var userId = AddUser();
        await _updates.HandleAsync(Message($"/start {await AddCodeAsync(userId)}", chatId: 1), CancellationToken.None);

        await _updates.HandleAsync(Message($"/start {await AddCodeAsync(userId)}", chatId: 2), CancellationToken.None);

        var link = Assert.Single(await LinksAsync());
        Assert.Equal((userId, 2L), (link.UserId, link.ChatId));
    }

    [Fact]
    public async Task ConnectingAChatThatBelongedToAnotherAccount_TakesItOver()
    {
        var first = AddUser();
        var second = AddUser();
        await _updates.HandleAsync(Message($"/start {await AddCodeAsync(first)}"), CancellationToken.None);

        await _updates.HandleAsync(Message($"/start {await AddCodeAsync(second)}"), CancellationToken.None);

        var link = Assert.Single(await LinksAsync());
        Assert.Equal((second, ChatId), (link.UserId, link.ChatId));
    }

    [Fact]
    public async Task PlainStart_ExplainsHowToConnect_InTheTelegramAppsLanguage()
    {
        await _updates.HandleAsync(Message("/start", language: "en-GB"), CancellationToken.None);

        var reply = Assert.Single(_bot.Sent);
        Assert.StartsWith("Hi! I'm the <b>IMOVA</b> bot.", reply.Html);
        Assert.Equal("Open settings", Assert.Single(reply.Buttons).Text);
    }

    [Fact]
    public async Task PlainStart_OnAConnectedChat_SaysWhichAccount()
    {
        var userId = AddUser();
        await _updates.HandleAsync(Message($"/start {await AddCodeAsync(userId)}"), CancellationToken.None);

        await _updates.HandleAsync(Message("/start"), CancellationToken.None);

        Assert.StartsWith($"Telegramul e conectat la contul IMOVA <b>{userId:N}@example.com</b>", _bot.Sent[1].Html);
    }

    [Fact]
    public async Task Stop_DisconnectsTheChat()
    {
        var userId = AddUser();
        await _updates.HandleAsync(Message($"/start {await AddCodeAsync(userId)}"), CancellationToken.None);

        await _updates.HandleAsync(Message("/stop"), CancellationToken.None);

        Assert.Empty(await LinksAsync());
        Assert.StartsWith("Am deconectat Telegramul", _bot.Sent[1].Html);
    }

    [Fact]
    public async Task Stop_OnAChatThatIsNotConnected_SaysSo()
    {
        await _updates.HandleAsync(Message("/stop"), CancellationToken.None);

        Assert.StartsWith("Acest Telegram nu e conectat", Assert.Single(_bot.Sent).Html);
    }

    [Fact]
    public async Task SavedSearches_ListsTheAccountsSearches_WithTheirAlerts()
    {
        var userId = AddUser();
        var search = SavedSearch.Create(userId, "Apartamente <Botanica>", "transactionType=Sale", AlertFrequency.Instant, Now);
        _dbContext.SavedSearches.Add(search);
        _dbContext.SavedSearches.Add(SavedSearch.Create(Guid.NewGuid(), "Someone else's", "transactionType=Rent", AlertFrequency.Daily, Now));
        await _dbContext.SaveChangesAsync();
        await _updates.HandleAsync(Message($"/start {await AddCodeAsync(userId)}"), CancellationToken.None);

        await _updates.HandleAsync(Message("/cautari"), CancellationToken.None);

        var reply = _bot.Sent[1];
        Assert.Contains($"<a href=\"https://imova.md/saved-searches/{search.Id}/open\">Apartamente &lt;Botanica&gt;</a> — Imediat", reply.Html);
        Assert.DoesNotContain("Someone else", reply.Html);
        Assert.Equal("https://imova.md/saved-searches", Assert.Single(reply.Buttons).Url);
    }

    [Fact]
    public async Task SavedSearches_WhenNotConnected_ExplainsHowToConnect()
    {
        await _updates.HandleAsync(Message("/cautari"), CancellationToken.None);

        Assert.StartsWith("Bună! Sunt botul", Assert.Single(_bot.Sent).Html);
    }

    [Fact]
    public async Task GroupChats_AreIgnored()
    {
        var code = await AddCodeAsync(AddUser());

        await _updates.HandleAsync(Message($"/start {code}", isPrivate: false), CancellationToken.None);

        Assert.Empty(_bot.Sent);
        Assert.Empty(await LinksAsync());
    }

    [Fact]
    public async Task AChatThatBlockedTheBot_IsDisconnected()
    {
        var userId = AddUser();
        await _updates.HandleAsync(Message($"/start {await AddCodeAsync(userId)}"), CancellationToken.None);
        _bot.UnavailableChats.Add(ChatId);

        await _updates.HandleAsync(Message("/cautari"), CancellationToken.None);

        Assert.Empty(await LinksAsync());
    }

    private static TelegramCallback Press(string? data, long chatId = ChatId) => new("cb-1", chatId, data, "ro");

    [Fact]
    public async Task StopAlertButton_TurnsThatSearchsAlertsOff_AndSaysSo()
    {
        var userId = AddUser();
        var search = SavedSearch.Create(userId, "Garaje", "transactionType=Sale", AlertFrequency.Instant, Now, AlertChannels.Telegram);
        _dbContext.SavedSearches.Add(search);
        await _dbContext.SaveChangesAsync();
        await _updates.HandleAsync(Message($"/start {await AddCodeAsync(userId)}"), CancellationToken.None);

        await _updates.HandleCallbackAsync(Press(TelegramBotUpdates.StopAlertData(search.Id)), CancellationToken.None);

        await using var fresh = TestDbContextFactory.Create(_databaseName);
        Assert.Equal(AlertFrequency.Off, (await fresh.SavedSearches.SingleAsync()).AlertFrequency);
        Assert.Equal(("cb-1", "Alertă oprită"), Assert.Single(_bot.Answers));
        Assert.StartsWith("Am oprit alertele pentru „Garaje”", _bot.Sent[^1].Html);
    }

    [Fact]
    public async Task StopAlertButton_ForAnotherAccountsSearch_ChangesNothing()
    {
        var userId = AddUser();
        var someoneElses = SavedSearch.Create(AddUser(), "Străin", "transactionType=Sale", AlertFrequency.Instant, Now);
        _dbContext.SavedSearches.Add(someoneElses);
        await _dbContext.SaveChangesAsync();
        await _updates.HandleAsync(Message($"/start {await AddCodeAsync(userId)}"), CancellationToken.None);

        await _updates.HandleCallbackAsync(Press(TelegramBotUpdates.StopAlertData(someoneElses.Id)), CancellationToken.None);

        await using var fresh = TestDbContextFactory.Create(_databaseName);
        Assert.Equal(AlertFrequency.Instant, (await fresh.SavedSearches.SingleAsync()).AlertFrequency);
        Assert.Equal("Această căutare nu mai există.", Assert.Single(_bot.Answers).Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("something-else")]
    [InlineData("ss-off:not-a-guid")]
    public async Task UnknownButtons_AreJustAcknowledged(string? data)
    {
        await _updates.HandleCallbackAsync(Press(data), CancellationToken.None);

        Assert.Equal(("cb-1", (string?)null), Assert.Single(_bot.Answers));
        Assert.Empty(_bot.Sent);
    }

    [Fact]
    public async Task StopAlertButton_FromAChatNoLongerConnected_SaysSo()
    {
        await _updates.HandleCallbackAsync(Press(TelegramBotUpdates.StopAlertData(Guid.NewGuid())), CancellationToken.None);

        Assert.Equal("Acest Telegram nu e conectat la niciun cont IMOVA.", Assert.Single(_bot.Answers).Text);
    }
}

