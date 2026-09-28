using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Imova.Contracts.Auth;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Auth;

// Login tokens are short-lived; the refresh token renews them (rotating each time), sign-out ends
// it, and so does anything that changes the security stamp.
public class RefreshSessionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "SuperSecret1!";
    private readonly WebApplicationFactory<Program> _factory;

    public RefreshSessionTests(WebApplicationFactory<Program> factory)
    {
        // No grace for a replayed token, so replay detection can be tested without waiting a minute.
        _factory = ListingApi.Configure(factory).WithWebHostBuilder(builder =>
            builder.UseSetting("Sessions:ReuseGraceSeconds", "0"));
    }

    private async Task<AuthResultDto> SignInAsync(bool rememberMe = true)
    {
        var (_, user) = await ListingApi.RegisterAsync(_factory);
        var login = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login", new { email = user.Email, password = Password, rememberMe });
        login.EnsureSuccessStatusCode();
        return (await login.Content.ReadFromJsonAsync<AuthResultDto>())!;
    }

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });

    private async Task<HttpStatusCode> MeAsync(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (await client.GetAsync("/api/v1/auth/me")).StatusCode;
    }

    private static async Task AssertSessionEndedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("auth.sessionExpired", body.RootElement.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData(true, 30 * 24)]
    [InlineData(false, 24)]
    public async Task Login_GivesAFifteenMinuteTokenAndARefreshTokenThatFollowsRememberMe(bool rememberMe, int refreshHours)
    {
        var session = await SignInAsync(rememberMe);

        Assert.Equal(rememberMe, session.Persistent);
        Assert.InRange(session.ExpiresAt - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15));
        Assert.InRange(
            session.RefreshTokenExpiresAt - DateTimeOffset.UtcNow,
            TimeSpan.FromHours(refreshHours) - TimeSpan.FromMinutes(1),
            TimeSpan.FromHours(refreshHours));
    }

    [Fact]
    public async Task Refresh_ReturnsANewPair_ThatWorks()
    {
        var session = await SignInAsync();

        var response = await RefreshAsync(session.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var renewed = (await response.Content.ReadFromJsonAsync<SessionTokenDto>())!;
        Assert.NotEqual(session.RefreshToken, renewed.RefreshToken);
        Assert.True(renewed.Persistent);
        Assert.Equal(HttpStatusCode.OK, await MeAsync(renewed.Token));
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(renewed.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task AReplayedRefreshToken_EndsTheSession()
    {
        var session = await SignInAsync();
        var renewed = (await (await RefreshAsync(session.RefreshToken)).Content.ReadFromJsonAsync<SessionTokenDto>())!;
        await Task.Delay(TimeSpan.FromMilliseconds(50));

        await AssertSessionEndedAsync(await RefreshAsync(session.RefreshToken));
        await AssertSessionEndedAsync(await RefreshAsync(renewed.RefreshToken));
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var session = await SignInAsync();

        var logout = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = session.RefreshToken });

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        await AssertSessionEndedAsync(await RefreshAsync(session.RefreshToken));
    }

    [Fact]
    public async Task ChangingThePassword_EndsOtherSessionsRefreshTokens_AndContinuesThisOne()
    {
        var (thisDevice, user) = await ListingApi.RegisterAsync(_factory);
        var login = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login", new { email = user.Email, password = Password, rememberMe = true });
        var otherDevice = (await login.Content.ReadFromJsonAsync<AuthResultDto>())!;

        var change = await thisDevice.PutAsJsonAsync(
            "/api/v1/auth/password", new { currentPassword = Password, newPassword = "BrandNewPass1!" });
        var result = (await change.Content.ReadFromJsonAsync<ChangePasswordResultDto>())!;

        await AssertSessionEndedAsync(await RefreshAsync(otherDevice.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(result.Session.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_WithAnUnknownToken_Is401_AndWithNone_400()
    {
        await AssertSessionEndedAsync(await RefreshAsync("made-up-token"));
        Assert.Equal(HttpStatusCode.BadRequest, (await RefreshAsync("")).StatusCode);
    }
}
