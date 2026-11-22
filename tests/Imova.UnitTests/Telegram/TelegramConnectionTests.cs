using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Telegram;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Telegram;

// The account side: status, a fresh "Conectează Telegram" link, disconnecting.
public class TelegramConnectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly TelegramOptions Options = new() { BotUsername = "ImovaDevBot" };

    [Fact]
    public async Task StartLink_GivesADeepLinkWhoseCodeIsStoredOnlyAsAHash()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = ListingTestData.AddUser(db, Guid.NewGuid()).Id;
        await db.SaveChangesAsync();

        var result = await new StartTelegramLinkHandler(db, Options, new ManualTimeProvider(Now))
            .Handle(new StartTelegramLinkCommand(userId), CancellationToken.None);

        Assert.StartsWith("https://t.me/ImovaDevBot?start=", result.Url);
        var code = result.Url["https://t.me/ImovaDevBot?start=".Length..];
        Assert.True(TelegramLinkCodes.LooksValid(code));
        Assert.Equal(Now + TimeSpan.FromMinutes(15), result.ExpiresAt);
        var stored = Assert.Single(db.TelegramLinkCodes);
        Assert.Equal(TelegramLinkCodes.Hash(code), stored.CodeHash);
        Assert.NotEqual(code, stored.CodeHash);
    }

    [Fact]
    public async Task StartLink_Again_ReplacesTheEarlierCode()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = ListingTestData.AddUser(db, Guid.NewGuid()).Id;
        await db.SaveChangesAsync();
        var handler = new StartTelegramLinkHandler(db, Options, new ManualTimeProvider(Now));

        await handler.Handle(new StartTelegramLinkCommand(userId), CancellationToken.None);
        var second = await handler.Handle(new StartTelegramLinkCommand(userId), CancellationToken.None);

        Assert.Equal(TelegramLinkCodes.Hash(second.Url.Split("start=")[1]), Assert.Single(db.TelegramLinkCodes).CodeHash);
    }

    [Fact]
    public async Task StartLink_WithoutABot_IsAConflict()
    {
        await using var db = TestDbContextFactory.Create();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            new StartTelegramLinkHandler(db, new TelegramOptions(), TimeProvider.System)
                .Handle(new StartTelegramLinkCommand(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal(ErrorCodes.TelegramUnavailable, ex.Code);
    }

    [Fact]
    public async Task Status_AndDisconnect()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = ListingTestData.AddUser(db, Guid.NewGuid()).Id;
        db.TelegramLinks.Add(new TelegramLink { UserId = userId, ChatId = 42, TelegramUsername = "ion_p", LinkedAt = Now });
        await db.SaveChangesAsync();

        var status = await new GetTelegramConnectionHandler(db, Options).Handle(new GetTelegramConnectionQuery(userId), CancellationToken.None);
        Assert.Equal((true, true, "ion_p", (DateTimeOffset?)Now), (status.Available, status.Connected, status.Username, status.ConnectedAt));

        await new DisconnectTelegramHandler(db).Handle(new DisconnectTelegramCommand(userId), CancellationToken.None);
        // Idempotent.
        await new DisconnectTelegramHandler(db).Handle(new DisconnectTelegramCommand(userId), CancellationToken.None);

        var after = await new GetTelegramConnectionHandler(db, Options).Handle(new GetTelegramConnectionQuery(userId), CancellationToken.None);
        Assert.False(after.Connected);
        Assert.Empty(db.TelegramLinks);
    }
}
