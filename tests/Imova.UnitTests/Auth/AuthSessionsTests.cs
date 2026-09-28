using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Auth.Sessions;
using Imova.Contracts.Auth;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Auth;

public class AuthSessionsTests
{
    private static readonly DateTimeOffset Start = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeUserStore _store = new();
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly ManualTimeProvider _clock = new(Start);
    private readonly FakeJwtTokenGenerator _jwt = new();
    private readonly AuthSessions _sessions;
    private readonly ApplicationUser _user;

    public AuthSessionsTests()
    {
        _userManager = TestUserManagerFactory.Create(_store);
        _sessions = TestSessions.For(_userManager, _db, _clock, _jwt);
        _user = _store.SeedUserWithPassword("ana@example.com", "Password1!");
    }

    private Task<SessionTokenDto> RefreshAsync(string token) => _sessions.RefreshAsync(token, CancellationToken.None);

    private Task<SessionTokenDto> StartAsync(bool persistent = true) =>
        _sessions.StartAsync(_user, persistent, CancellationToken.None);

    private async Task AssertEndedAsync(string token)
    {
        var error = await Assert.ThrowsAsync<AuthenticationFailedException>(() => RefreshAsync(token));
        Assert.Equal("auth.sessionExpired", error.Code);
    }

    [Fact]
    public async Task ARememberedSession_LastsThirtyDaysFromItsLastUse()
    {
        var session = await StartAsync();

        Assert.True(session.Persistent);
        Assert.Equal(_clock.Now.AddDays(30), session.RefreshTokenExpiresAt);
        Assert.NotNull(_jwt.LastSessionId);

        // 29 days later it's still good, and using it starts a new 30 days.
        _clock.Advance(TimeSpan.FromDays(29));
        var refreshed = await RefreshAsync(session.RefreshToken);
        Assert.Equal(_clock.Now.AddDays(30), refreshed.RefreshTokenExpiresAt);
    }

    [Fact]
    public async Task ASessionWithoutRememberMe_LastsTwentyFourHoursUnused()
    {
        var session = await StartAsync(persistent: false);

        Assert.False(session.Persistent);
        Assert.Equal(_clock.Now.AddHours(24), session.RefreshTokenExpiresAt);

        _clock.Advance(TimeSpan.FromHours(25));
        await AssertEndedAsync(session.RefreshToken);
    }

    [Fact]
    public async Task AnUnusedRememberedSession_EndsAfterThirtyDays()
    {
        var session = await StartAsync();

        _clock.Advance(TimeSpan.FromDays(31));

        await AssertEndedAsync(session.RefreshToken);
    }

    [Fact]
    public async Task EvenAConstantlyUsedSession_EndsAfterNinetyDays()
    {
        var token = (await StartAsync()).RefreshToken;

        // Used every week for 12 weeks (84 days)…
        for (var week = 0; week < 12; week++)
        {
            _clock.Advance(TimeSpan.FromDays(7));
            var refreshed = await RefreshAsync(token);
            Assert.True(refreshed.RefreshTokenExpiresAt <= Start.AddDays(90));
            token = refreshed.RefreshToken;
        }

        // …and a week later (day 91) it's over anyway.
        _clock.Advance(TimeSpan.FromDays(7));
        await AssertEndedAsync(token);
    }

    [Fact]
    public async Task Refreshing_KeepsTheSessionAndRememberMe_AndIssuesANewToken()
    {
        var session = await StartAsync();
        var sessionId = _jwt.LastSessionId;

        var refreshed = await RefreshAsync(session.RefreshToken);

        Assert.NotEqual(session.RefreshToken, refreshed.RefreshToken);
        Assert.True(refreshed.Persistent);
        Assert.Equal(sessionId, _jwt.LastSessionId);
    }

    [Fact]
    public async Task OnlyAHashOfTheTokenIsStored()
    {
        var session = await StartAsync();

        var stored = Assert.Single(_db.RefreshTokens);
        Assert.NotEqual(session.RefreshToken, stored.TokenHash);
        Assert.Equal(AuthSessions.Hash(session.RefreshToken), stored.TokenHash);
    }

    [Fact]
    public async Task AReplacedTokenUsedAgainRightAway_IsARace_NotTheft()
    {
        var session = await StartAsync();
        var first = await RefreshAsync(session.RefreshToken);

        _clock.Advance(TimeSpan.FromSeconds(20));
        var second = await RefreshAsync(session.RefreshToken);

        // Both continuations work.
        await RefreshAsync(first.RefreshToken);
        await RefreshAsync(second.RefreshToken);
    }

    [Fact]
    public async Task AReplacedTokenUsedAgainLater_EndsTheWholeSession()
    {
        var session = await StartAsync();
        var legitimate = await RefreshAsync(session.RefreshToken);

        _clock.Advance(TimeSpan.FromMinutes(5));
        await AssertEndedAsync(session.RefreshToken);

        // The thief is out, and so is the owner's current token: they sign in again.
        await AssertEndedAsync(legitimate.RefreshToken);
    }

    [Fact]
    public async Task ReplayingOneSession_LeavesTheUsersOtherSessionsAlone()
    {
        var phone = await StartAsync();
        var laptop = await StartAsync();
        await RefreshAsync(phone.RefreshToken);

        _clock.Advance(TimeSpan.FromMinutes(5));
        await AssertEndedAsync(phone.RefreshToken);

        await RefreshAsync(laptop.RefreshToken);
    }

    [Fact]
    public async Task AChangedSecurityStamp_EndsEverySession()
    {
        var session = await StartAsync();

        await _userManager.UpdateSecurityStampAsync(_user);

        await AssertEndedAsync(session.RefreshToken);
    }

    [Fact]
    public async Task Continue_KeepsTheCallersSessionAfterAStampChange_AndEndsTheOthers()
    {
        var mine = await StartAsync();
        var mySessionId = _jwt.LastSessionId;
        var other = await StartAsync();

        await _userManager.UpdateSecurityStampAsync(_user);
        var continued = await _sessions.ContinueAsync(_user, mySessionId, CancellationToken.None);

        Assert.True(continued.Persistent);
        Assert.Equal(mySessionId, _jwt.LastSessionId);
        await RefreshAsync(continued.RefreshToken);
        await AssertEndedAsync(other.RefreshToken);
        await AssertEndedAsync(mine.RefreshToken);
    }

    [Fact]
    public async Task Continue_WithoutAKnownSession_StartsAShortOne()
    {
        var continued = await _sessions.ContinueAsync(_user, Guid.NewGuid(), CancellationToken.None);

        Assert.False(continued.Persistent);
        await RefreshAsync(continued.RefreshToken);
    }

    [Fact]
    public async Task SigningOut_EndsTheSession_AndIsHarmlessForUnknownTokens()
    {
        var session = await StartAsync();
        var refreshed = await RefreshAsync(session.RefreshToken);

        await _sessions.EndAsync(session.RefreshToken, CancellationToken.None);
        await _sessions.EndAsync("not-a-token", CancellationToken.None);

        await AssertEndedAsync(refreshed.RefreshToken);
        Assert.All(await _db.RefreshTokens.ToListAsync(), t => Assert.NotNull(t.RevokedAt));
    }

    [Fact]
    public async Task AnUnknownToken_IsRefused()
    {
        await AssertEndedAsync("made-up");
    }

    [Fact]
    public async Task ADeletedAccount_CannotRefresh()
    {
        var session = await StartAsync();

        await _userManager.DeleteAsync(_user);

        await AssertEndedAsync(session.RefreshToken);
    }
}
