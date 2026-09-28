using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Auth.SignOutOtherSessions;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Auth;

public class SignOutOtherSessionsHandlerTests
{
    [Fact]
    public async Task Handle_ChangesTheSecurityStamp_AndReturnsATokenForTheCaller()
    {
        var store = new FakeUserStore();
        var user = store.SeedUserWithPassword("user@example.com", "Password1!");
        var stampBefore = user.SecurityStamp;
        var tokens = new FakeJwtTokenGenerator();

        var userManager = TestUserManagerFactory.Create(store);
        var result = await new SignOutOtherSessionsHandler(userManager, TestSessions.For(userManager, jwt: tokens))
            .Handle(new SignOutOtherSessionsCommand(user.Id), CancellationToken.None);

        Assert.NotEqual(stampBefore, user.SecurityStamp);
        Assert.Equal("fake-token", result.Token);
        // The new token is made after the stamp changed, so it carries the new one.
        Assert.Same(user, tokens.LastUser);
    }

    [Fact]
    public async Task Handle_ForUnknownUser_ThrowsAuthenticationFailedException()
    {
        var userManager = TestUserManagerFactory.Create(new FakeUserStore());
        var handler = new SignOutOtherSessionsHandler(userManager, TestSessions.For(userManager));

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.Handle(new SignOutOtherSessionsCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
