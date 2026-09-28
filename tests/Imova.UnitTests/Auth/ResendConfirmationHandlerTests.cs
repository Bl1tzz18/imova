using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Auth;
using Imova.Application.Features.Auth.ResendConfirmation;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Auth;

public class ResendConfirmationHandlerTests
{
    private readonly FakeUserStore _store = new();
    private readonly FakeEmailSender _email = new();
    private readonly ManualTimeProvider _clock = new(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
    private readonly AuthEmailThrottle _throttle;

    public ResendConfirmationHandlerTests()
    {
        _throttle = new AuthEmailThrottle(_clock);
    }

    private Task ResendAsync(Guid userId)
    {
        var userManager = TestUserManagerFactory.Create(_store);
        return new ResendConfirmationHandler(userManager, new AccountEmails(userManager, _email, new AppOptions()), _throttle)
            .Handle(new ResendConfirmationCommand(userId), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ForAnUnconfirmedUser_SendsANewConfirmationLink()
    {
        var user = _store.SeedUser("ana@example.com", emailConfirmed: false);

        await ResendAsync(user.Id);

        var email = Assert.Single(_email.Sent);
        Assert.Equal("ana@example.com", email.To);
        Assert.Contains($"/confirm-email?userId={user.Id}&token=", email.TextBody);
    }

    [Fact]
    public async Task Handle_ForAConfirmedUser_SendsNothing()
    {
        var user = _store.SeedUser("ana@example.com", emailConfirmed: true);

        await ResendAsync(user.Id);

        Assert.Empty(_email.Sent);
    }

    [Fact]
    public async Task Handle_AskingAgainWithinTheCooldown_IsRejected()
    {
        var user = _store.SeedUser("ana@example.com", emailConfirmed: false);
        await ResendAsync(user.Id);

        var error = await Assert.ThrowsAsync<TooManyRequestsException>(() => ResendAsync(user.Id));

        Assert.Equal(ResendConfirmationHandler.TooSoon, error.Message);
        Assert.Single(_email.Sent);
    }
}
