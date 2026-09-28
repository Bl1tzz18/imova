using Imova.Application.Features.Auth;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Auth;

public class AuthEmailThrottleTests
{
    private readonly ManualTimeProvider _clock = new(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void TryAcquire_AllowsOneEmailPerKeyPerCooldown()
    {
        var throttle = new AuthEmailThrottle(_clock);

        Assert.True(throttle.TryAcquire("password-reset:1"));
        Assert.False(throttle.TryAcquire("password-reset:1"));

        _clock.Advance(AuthEmailThrottle.Cooldown - TimeSpan.FromSeconds(1));
        Assert.False(throttle.TryAcquire("password-reset:1"));

        _clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(throttle.TryAcquire("password-reset:1"));
    }

    [Fact]
    public void TryAcquire_KeysAreIndependent()
    {
        var throttle = new AuthEmailThrottle(_clock);

        Assert.True(throttle.TryAcquire("password-reset:1"));
        Assert.True(throttle.TryAcquire("password-reset:2"));
        Assert.True(throttle.TryAcquire("email-confirmation:1"));
    }
}
