using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Auth.ChangePassword;
using Imova.UnitTests.TestSupport;
using Microsoft.AspNetCore.Identity;

namespace Imova.UnitTests.Auth;

public class ChangePasswordHandlerTests
{
    private static ChangePasswordHandler CreateHandler(FakeUserStore store) =>
        new(TestUserManagerFactory.Create(store));

    [Fact]
    public async Task Handle_WithCorrectCurrentPassword_ChangesPassword()
    {
        var store = new FakeUserStore();
        var user = store.SeedUserWithPassword("user@example.com", "OldPassword1");
        var handler = CreateHandler(store);

        var result = await handler.Handle(
            new ChangePasswordCommand(user.Id, "OldPassword1", "NewPassword1"), CancellationToken.None);

        Assert.True(result.HasPassword);
        var verification = new PasswordHasher<ApplicationUser>().VerifyHashedPassword(user, user.PasswordHash!, "NewPassword1");
        Assert.Equal(PasswordVerificationResult.Success, verification);
    }

    [Fact]
    public async Task Handle_WithWrongCurrentPassword_ThrowsValidationException()
    {
        var store = new FakeUserStore();
        var user = store.SeedUserWithPassword("user@example.com", "OldPassword1");
        var handler = CreateHandler(store);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new ChangePasswordCommand(user.Id, "WrongPassword1", "NewPassword1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForAccountWithExistingPasswordButNoCurrentPasswordSupplied_ThrowsValidationException()
    {
        var store = new FakeUserStore();
        var user = store.SeedUserWithPassword("user@example.com", "OldPassword1");
        var handler = CreateHandler(store);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new ChangePasswordCommand(user.Id, null, "NewPassword1"), CancellationToken.None));

        Assert.Contains(exception.Errors, e => e.PropertyName == nameof(ChangePasswordCommand.CurrentPassword));
    }

    [Fact]
    public async Task Handle_ForAccountWithNoExistingPassword_SetsPasswordWithoutRequiringCurrent()
    {
        // Google-only account (see GoogleLoginHandler) — "change password" here really means
        // "set a password for the first time".
        var store = new FakeUserStore();
        var user = store.SeedUser("google-only@example.com", emailConfirmed: true);
        var handler = CreateHandler(store);

        var result = await handler.Handle(new ChangePasswordCommand(user.Id, null, "NewPassword1"), CancellationToken.None);

        Assert.True(result.HasPassword);
        Assert.NotNull(user.PasswordHash);
    }

    [Fact]
    public async Task Handle_ForUnknownUserId_ThrowsAuthenticationFailedException()
    {
        var store = new FakeUserStore();
        var handler = CreateHandler(store);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.Handle(new ChangePasswordCommand(Guid.NewGuid(), null, "NewPassword1"), CancellationToken.None));
    }
}
