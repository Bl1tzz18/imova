using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Telegram;
using Imova.Application.Features.Telegram.Bot;
using Imova.Contracts.Account;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Imova.IntegrationTests.Telegram;

// Connecting Telegram through the real endpoints and database: the link from the account page,
// the bot's /start with it (run here the way the Worker runs it, against a fake Telegram), taking a
// chat over from another account (the unique chat index), and what account deletion leaves.
public class TelegramEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string LinkPrefix = "https://t.me/ImovaTestBot?start=";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly WebApplicationFactory<Program> _factoryWithoutBot;

    public TelegramEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factoryWithoutBot = ListingApi.Configure(factory);
        _factory = _factoryWithoutBot.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<TelegramOptions>();
            services.AddSingleton(new TelegramOptions { BotUsername = "ImovaTestBot" });
        }));
    }

    private async Task<string> StartLinkAsync(HttpClient client)
    {
        var response = await client.PostAsync("/api/v1/users/me/telegram/link", null);
        response.EnsureSuccessStatusCode();
        var link = (await response.Content.ReadFromJsonAsync<TelegramLinkStartDto>())!;
        Assert.StartsWith(LinkPrefix, link.Url);
        return link.Url[LinkPrefix.Length..];
    }

    // What the Worker does when someone presses Start on the link in Telegram.
    private async Task<List<string>> PressStartAsync(string code, long chatId)
    {
        var bot = new RecordingTelegramBot();
        using var scope = _factory.Services.CreateScope();
        var updates = new TelegramBotUpdates(
            scope.ServiceProvider.GetRequiredService<IApplicationDbContext>(), bot,
            new AppOptions { WebBaseUrl = "https://imova.md" }, TimeProvider.System);
        await updates.HandleAsync(new TelegramIncomingMessage(chatId, true, $"/start {code}", "tester", "Test", "ro"), CancellationToken.None);
        return bot.Sent.Select(m => m.Html).ToList();
    }

    [Fact]
    public async Task ConnectWithTheLink_ShowsAsConnected_ThenDisconnect()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var before = await client.GetFromJsonAsync<TelegramConnectionDto>("/api/v1/users/me/telegram");
        Assert.Equal((true, false), (before!.Available, before.Connected));

        var chatId = Random.Shared.NextInt64(1_000_000, long.MaxValue);
        var replies = await PressStartAsync(await StartLinkAsync(client), chatId);

        Assert.StartsWith("Gata! Telegramul e conectat", Assert.Single(replies));
        var connected = await client.GetFromJsonAsync<TelegramConnectionDto>("/api/v1/users/me/telegram");
        Assert.Equal((true, "tester"), (connected!.Connected, connected.Username));

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/v1/users/me/telegram")).StatusCode);
        Assert.False((await client.GetFromJsonAsync<TelegramConnectionDto>("/api/v1/users/me/telegram"))!.Connected);
    }

    [Fact]
    public async Task AChatConnectedToAnotherAccount_MovesToTheNewOne()
    {
        var (first, firstUser) = await ListingApi.RegisterAsync(_factory);
        var (second, secondUser) = await ListingApi.RegisterAsync(_factory);
        var chatId = Random.Shared.NextInt64(1_000_000, long.MaxValue);
        await PressStartAsync(await StartLinkAsync(first), chatId);

        await PressStartAsync(await StartLinkAsync(second), chatId);

        await using var db = ListingApi.NewDbContext();
        var link = await db.TelegramLinks.SingleAsync(l => l.ChatId == chatId);
        Assert.Equal(secondUser.Id, link.UserId);
        Assert.False(await db.TelegramLinks.AnyAsync(l => l.UserId == firstUser.Id));
        Assert.False((await first.GetFromJsonAsync<TelegramConnectionDto>("/api/v1/users/me/telegram"))!.Connected);
    }

    [Fact]
    public async Task DeletingTheAccount_RemovesItsTelegramConnection()
    {
        var (client, user) = await ListingApi.RegisterAsync(_factory);
        await PressStartAsync(await StartLinkAsync(client), Random.Shared.NextInt64(1_000_000, long.MaxValue));
        await StartLinkAsync(client);

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/users/me")
        {
            Content = JsonContent.Create(new { password = "SuperSecret1!" }),
        });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var db = ListingApi.NewDbContext();
        Assert.False(await db.TelegramLinks.AnyAsync(l => l.UserId == user.Id));
        Assert.False(await db.TelegramLinkCodes.AnyAsync(c => c.UserId == user.Id));
    }

    [Fact]
    public async Task WithoutABot_TheLinkIsRefused()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factoryWithoutBot);

        Assert.False((await client.GetFromJsonAsync<TelegramConnectionDto>("/api/v1/users/me/telegram"))!.Available);
        var response = await client.PostAsync("/api/v1/users/me/telegram/link", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("telegram.unavailable", problem.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task SignedOut_IsUnauthorized()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/users/me/telegram")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/users/me/telegram/link", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync("/api/v1/users/me/telegram")).StatusCode);
    }
}
