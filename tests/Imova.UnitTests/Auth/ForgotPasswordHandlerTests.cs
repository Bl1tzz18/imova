using Imova.Application.Common;
using Imova.Application.Features.Auth;
using Imova.Application.Features.Auth.ForgotPassword;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Auth;

public class ForgotPasswordHandlerTests
{
    private readonly FakeUserStore _store = new();
    private readonly FakeEmailSender _email = new();
    private readonly ManualTimeProvider _clock = new(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
    private readonly AuthEmailThrottle _throttle;

    public ForgotPasswordHandlerTests()
    {
        _throttle = new AuthEmailThrottle(_clock);
    }

    private Task SendAsync(string email)
    {
        var userManager = TestUserManagerFactory.Create(_store);
        var handler = new ForgotPasswordHandler(
            userManager,
            new AccountEmails(userManager, _email, new AppOptions { WebBaseUrl = "https://imova.test/" }),
            _throttle,
            NullLogger<ForgotPasswordHandler>.Instance);
        return handler.Handle(new ForgotPasswordCommand(email), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ForAnExistingAccount_EmailsAResetLink()
    {
        var user = _store.SeedUserWithPassword("ana+test@example.com", "OldPassword1");

        await SendAsync("ana+test@example.com");

        var email = Assert.Single(_email.Sent);
        Assert.Equal("ana+test@example.com", email.To);
        var token = AccountTokens.Encode(FakeTokenProvider.TokenFor("ResetPassword", user));
        Assert.Contains($"https://imova.test/reset-password?email=ana%2Btest%40example.com&token={token}", email.TextBody);
    }

    [Fact]
    public async Task Handle_ForAnUnknownEmail_SendsNothingAndDoesNotFail()
    {
        await SendAsync("nobody@example.com");

        Assert.Empty(_email.Sent);
    }

    [Fact]
    public async Task Handle_ForAGoogleOnlyAccount_StillEmailsALink()
    {
        // No password yet — resetting is how such an account sets its first one.
        _store.SeedUser("google@example.com", emailConfirmed: true);

        await SendAsync("google@example.com");

        Assert.Single(_email.Sent);
    }

    [Fact]
    public async Task Handle_AskingAgainWithinTheCooldown_DoesNotSendASecondEmail()
    {
        _store.SeedUserWithPassword("ana@example.com", "OldPassword1");

        await SendAsync("ana@example.com");
        await SendAsync("ana@example.com");
        Assert.Single(_email.Sent);

        _clock.Advance(AuthEmailThrottle.Cooldown);
        await SendAsync("ana@example.com");
        Assert.Equal(2, _email.Sent.Count);
    }

    [Fact]
    public async Task Handle_WhenSendingFails_DoesNotSurfaceTheError()
    {
        // Failing only for real accounts would tell a caller which emails are registered.
        _store.SeedUserWithPassword("ana@example.com", "OldPassword1");
        _email.Fail = true;

        await SendAsync("ana@example.com");
    }
}
