using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Auth;
using Imova.Infrastructure.Identity;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Imova.IntegrationTests.Auth;

// A password change, a password reset or "sign out other sessions" ends every existing session
// (their tokens stop working at once), while the session that made the change keeps going.
public class SessionRevocationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "SuperSecret1!";
    private readonly CapturingEmailSender _email = new();
    private readonly WebApplicationFactory<Program> _factory;

    public SessionRevocationTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory).WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(_email);
        }));
    }

    // A second, independent session for the same account (another browser/device).
    private async Task<HttpClient> SignInAgainAsync(string email, string password = Password)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        UseToken(client, (await login.Content.ReadFromJsonAsync<AuthResultDto>())!.Token);
        return client;
    }

    private static void UseToken(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private static async Task<HttpStatusCode> MeAsync(HttpClient client) =>
        (await client.GetAsync("/api/v1/auth/me")).StatusCode;

    [Fact]
    public async Task ChangingThePassword_SignsOutOtherSessions_ButNotThisOne()
    {
        var (thisDevice, user) = await ListingApi.RegisterAsync(_factory);
        var otherDevice = await SignInAgainAsync(user.Email);
        Assert.Equal(HttpStatusCode.OK, await MeAsync(otherDevice));

        var change = await thisDevice.PutAsJsonAsync(
            "/api/v1/auth/password", new { currentPassword = Password, newPassword = "BrandNewPass1!" });
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        var result = (await change.Content.ReadFromJsonAsync<ChangePasswordResultDto>())!;

        Assert.Equal(HttpStatusCode.Unauthorized, await MeAsync(otherDevice));
        // The token this device used before is revoked too — it continues with the one it got back.
        Assert.Equal(HttpStatusCode.Unauthorized, await MeAsync(thisDevice));
        UseToken(thisDevice, result.Token);
        Assert.Equal(HttpStatusCode.OK, await MeAsync(thisDevice));
    }

    [Fact]
    public async Task ResettingThePassword_SignsOutEverySession()
    {
        var (session, user) = await ListingApi.RegisterAsync(_factory);
        var anonymous = _factory.CreateClient();

        await anonymous.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = user.Email });
        var link = _email.LinkQuery(user.Email);
        var reset = await anonymous.PostAsJsonAsync(
            "/api/v1/auth/reset-password", new { email = user.Email, token = link["token"], newPassword = "BrandNewPass1!" });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, await MeAsync(session));
        Assert.Equal(HttpStatusCode.OK, await MeAsync(await SignInAgainAsync(user.Email, "BrandNewPass1!")));
    }

    [Fact]
    public async Task SignOutOtherSessions_KeepsOnlyTheCallerSignedIn()
    {
        var (thisDevice, user) = await ListingApi.RegisterAsync(_factory);
        var otherDevice = await SignInAgainAsync(user.Email);

        var response = await thisDevice.PostAsync("/api/v1/auth/sign-out-other-sessions", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        UseToken(thisDevice, (await response.Content.ReadFromJsonAsync<SessionTokenDto>())!.Token);

        Assert.Equal(HttpStatusCode.OK, await MeAsync(thisDevice));
        Assert.Equal(HttpStatusCode.Unauthorized, await MeAsync(otherDevice));

        // Signing in again afterwards works as usual.
        Assert.Equal(HttpStatusCode.OK, await MeAsync(await SignInAgainAsync(user.Email)));
    }

    [Fact]
    public async Task SignOutOtherSessions_RequiresSignIn()
    {
        var response = await _factory.CreateClient().PostAsync("/api/v1/auth/sign-out-other-sessions", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AValidlySignedTokenWithoutTheSessionClaim_IsRefused()
    {
        // What every token issued before this check looked like.
        var (_, user) = await ListingApi.RegisterAsync(_factory);
        var options = _factory.Services.GetRequiredService<JwtOptions>();
        var legacy = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(JwtRegisteredClaimNames.Email, user.Email)],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)), SecurityAlgorithms.HmacSha256));
        var client = _factory.CreateClient();
        UseToken(client, new JwtSecurityTokenHandler().WriteToken(legacy));

        Assert.Equal(HttpStatusCode.Unauthorized, await MeAsync(client));
    }
}
