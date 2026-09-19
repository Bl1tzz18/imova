using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Auth.Login;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Auth;

public class LoginHandlerTests
{
    private static LoginHandler CreateHandler(FakeUserStore store) =>
        new(TestUserManagerFactory.Create(store), new FakeJwtTokenGenerator());

    [Fact]
    public async Task Handle_WithCorrectCredentials_ReturnsAuthResult()
    {
        var store = new FakeUserStore();
        var user = store.SeedUserWithPassword("user@example.com", "CorrectPassword1");
        var handler = CreateHandler(store);

        var result = await handler.Handle(new LoginCommand("user@example.com", "CorrectPassword1"), CancellationToken.None);

        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal("user@example.com", result.User.Email);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsAuthenticationFailedException()
    {
        var store = new FakeUserStore();
        store.SeedUserWithPassword("user@example.com", "CorrectPassword1");
        var handler = CreateHandler(store);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.Handle(new LoginCommand("user@example.com", "WrongPassword1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ThrowsAuthenticationFailedException()
    {
        var store = new FakeUserStore();
        var handler = CreateHandler(store);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.Handle(new LoginCommand("nobody@example.com", "AnyPassword1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForAccountWithNoPassword_ThrowsAuthenticationFailedException()
    {
        // A Google-only account (see GoogleLoginHandler) has no password hash — logging in with
        // email/password must fail cleanly rather than throw an unrelated error.
        var store = new FakeUserStore();
        store.SeedUser("google-only@example.com", emailConfirmed: true);
        var handler = CreateHandler(store);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.Handle(new LoginCommand("google-only@example.com", "AnyPassword1"), CancellationToken.None));
    }
}
