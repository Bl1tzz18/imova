using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Auth.UpdateProfile;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Auth;

public class UpdateProfileHandlerTests
{
    private static UpdateProfileHandler CreateHandler(FakeUserStore store) =>
        new(TestUserManagerFactory.Create(store));

    [Fact]
    public async Task Handle_WithValidData_UpdatesDisplayNameAndPhoneNumber()
    {
        var store = new FakeUserStore();
        var user = store.SeedUser("user@example.com", emailConfirmed: true);
        var handler = CreateHandler(store);

        var result = await handler.Handle(
            new UpdateProfileCommand(user.Id, "New Name", "+373 69 123 456"), CancellationToken.None);

        Assert.Equal("New Name", user.DisplayName);
        Assert.Equal("+373 69 123 456", user.PhoneNumber);
        Assert.Equal("New Name", result.DisplayName);
        Assert.Equal("+373 69 123 456", result.PhoneNumber);
    }

    [Fact]
    public async Task Handle_WithNullDisplayName_ClearsDisplayName()
    {
        var store = new FakeUserStore();
        var user = store.SeedUser("user@example.com", emailConfirmed: true);
        user.DisplayName = "Old Name";
        var handler = CreateHandler(store);

        var result = await handler.Handle(new UpdateProfileCommand(user.Id, null, "+373 69 123 456"), CancellationToken.None);

        Assert.Null(user.DisplayName);
        Assert.Null(result.DisplayName);
    }

    [Fact]
    public async Task Handle_ForUnknownUserId_ThrowsAuthenticationFailedException()
    {
        var store = new FakeUserStore();
        var handler = CreateHandler(store);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.Handle(new UpdateProfileCommand(Guid.NewGuid(), "Name", "+373 69 123 456"), CancellationToken.None));
    }
}
