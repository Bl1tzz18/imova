using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Auth;
using Imova.Contracts.Listings;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Imova.IntegrationTests.Auth;

// Password reset, email confirmation (and its effect on listing review), login lockout, and the
// auth rate limit — through the real endpoints, with emails captured instead of sent.
public class AccountEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "SuperSecret1!";
    private readonly WebApplicationFactory<Program> _baseFactory;
    private readonly CapturingEmailSender _email = new();
    private readonly WebApplicationFactory<Program> _factory;

    public AccountEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
        _factory = WithCapturedEmail(ListingApi.Configure(factory));
    }

    private WebApplicationFactory<Program> WithCapturedEmail(WebApplicationFactory<Program> factory) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(_email);
        }));

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

    [Fact]
    public async Task ForgotThenResetPassword_SignsInWithTheNewPasswordOnly()
    {
        var (_, user) = await ListingApi.RegisterAsync(_factory);
        var client = _factory.CreateClient();

        var forgot = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = user.Email });
        Assert.Equal(HttpStatusCode.NoContent, forgot.StatusCode);

        var link = _email.LinkQuery(user.Email);
        Assert.Equal(user.Email, link["email"]);
        var reset = await client.PostAsJsonAsync(
            "/api/v1/auth/reset-password", new { email = user.Email, token = link["token"], newPassword = "BrandNewPass1!" });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, user.Email, Password)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, user.Email, "BrandNewPass1!")).StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_ForAnUnknownEmail_AnswersTheSameAndSendsNothing()
    {
        var email = $"nobody-{Guid.NewGuid():N}@example.com";

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(_email.SentTo(email));
    }

    [Fact]
    public async Task ResetPassword_WithAForgedToken_Returns400AndKeepsTheOldPassword()
    {
        var (_, user) = await ListingApi.RegisterAsync(_factory);
        var client = _factory.CreateClient();

        var reset = await client.PostAsJsonAsync(
            "/api/v1/auth/reset-password", new { email = user.Email, token = "Zm9yZ2Vk", newPassword = "BrandNewPass1!" });

        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, user.Email, Password)).StatusCode);
    }

    [Fact]
    public async Task UnconfirmedOwner_ListingWaitsAsDraft_UntilTheEmailedLinkIsOpened()
    {
        var (owner, user) = await ListingApi.RegisterAsync(_factory, confirmEmail: false);
        Assert.False(user.EmailConfirmed);

        var created = await owner.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(owner));
        created.EnsureSuccessStatusCode();
        var listing = (await created.Content.ReadFromJsonAsync<ListingDto>())!;
        Assert.Equal("Draft", listing.Status);

        var submit = await owner.PostAsync($"/api/v1/listings/{listing.Id}/submit-for-review", null);
        Assert.Equal(HttpStatusCode.Forbidden, submit.StatusCode);

        // The confirmation link was emailed at registration.
        var link = _email.LinkQuery(user.Email);
        var confirm = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/confirm-email", new { userId = link["userId"], token = link["token"] });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        var stored = (await owner.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{listing.Id}"))!;
        Assert.Equal("PendingReview", stored.Status);
        var profile = (await owner.GetFromJsonAsync<UserProfileDto>("/api/v1/auth/me"))!;
        Assert.True(profile.EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmail_WithAForgedToken_Returns400()
    {
        var (_, user) = await ListingApi.RegisterAsync(_factory, confirmEmail: false);

        var confirm = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/confirm-email", new { userId = user.Id, token = "Zm9yZ2Vk" });

        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);
    }

    [Fact]
    public async Task ResendConfirmation_TwiceInAMinute_Returns429TheSecondTime()
    {
        var (client, user) = await ListingApi.RegisterAsync(_factory, confirmEmail: false);

        // Registration itself didn't go through the throttle, so the first resend goes out.
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/v1/auth/resend-confirmation", null)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsync("/api/v1/auth/resend-confirmation", null)).StatusCode);
        Assert.Equal(2, _email.SentTo(user.Email).Count);
    }

    [Fact]
    public async Task Register_WithATakenEmail_ReportsItOnce_WithACode()
    {
        var (_, user) = await ListingApi.RegisterAsync(_factory);

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = user.Email,
            password = Password,
            displayName = "Again",
            phoneNumber = "+373 69 123 456",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = Assert.Single(problem.GetProperty("errorCodes").EnumerateArray());
        Assert.Equal("identity.DuplicateEmail", error.GetProperty("code").GetString());
        Assert.Equal("Email", error.GetProperty("field").GetString());
        Assert.Single(problem.GetProperty("errors").GetProperty("Email").EnumerateArray());
    }

    [Fact]
    public async Task Login_WithAWrongPassword_Returns401_WithACode()
    {
        var (_, user) = await ListingApi.RegisterAsync(_factory);

        var response = await LoginAsync(_factory.CreateClient(), user.Email, "WrongPassword1!");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("auth.invalidCredentials", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ResendConfirmation_RequiresSignIn()
    {
        var response = await _factory.CreateClient().PostAsync("/api/v1/auth/resend-confirmation", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithoutAPhoneNumber_Returns400NotA500()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = $"no-phone-{Guid.NewGuid():N}@example.com",
            password = Password,
            displayName = "No Phone",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("short1!")]
    [InlineData("NoNumbers!!")]
    [InlineData("NoSpecial123")]
    public async Task Register_WithAPasswordBreakingThePolicy_Returns400OnPassword(string password)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = $"weak-{Guid.NewGuid():N}@example.com",
            password,
            displayName = "Weak Password",
            phoneNumber = "+373 69 123 456",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"Password\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_AfterFiveWrongPasswords_IsLockedEvenWithTheRightOne()
    {
        var (_, user) = await ListingApi.RegisterAsync(_factory);
        var client = _factory.CreateClient();

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, user.Email, "WrongPassword1")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await LoginAsync(client, user.Email, "WrongPassword1")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await LoginAsync(client, user.Email, Password)).StatusCode);
    }

    [Fact]
    public async Task AuthEndpoints_AreRateLimitedPerClient()
    {
        var limited = WithCapturedEmail(ListingApi.Configure(_baseFactory).WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Auth:Enabled", "true");
            builder.UseSetting("RateLimiting:Auth:PermitLimit", "3");
            builder.UseSetting("RateLimiting:Auth:WindowSeconds", "60");
        }));
        var client = limited.CreateClient();
        var email = $"nobody-{Guid.NewGuid():N}@example.com";

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, email, "WrongPassword1")).StatusCode);
        }

        var blocked = await LoginAsync(client, email, "WrongPassword1");
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.True(blocked.Headers.Contains("Retry-After"));
        Assert.Equal("rateLimited", (await blocked.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // Other endpoints stay reachable.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }
}
