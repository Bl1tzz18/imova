using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Auth.UpdatePhoneNumber;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Auth;

public class UpdatePhoneNumberHandlerTests
{
    private static UpdatePhoneNumberHandler CreateHandler(FakeUserStore store) =>
        new(TestUserManagerFactory.Create(store));

    [Fact]
    public async Task Handle_WithValidPhoneNumber_UpdatesUserAndReturnsRequiresPhoneNumberFalse()
    {
        var store = new FakeUserStore();
        var user = store.SeedUser("user@example.com", emailConfirmed: true);
        var handler = CreateHandler(store);

        var result = await handler.Handle(new UpdatePhoneNumberCommand(user.Id, "+373 69 123 456"), CancellationToken.None);

        Assert.Equal("+373 69 123 456", user.PhoneNumber);
        Assert.False(result.RequiresPhoneNumber);
    }

    [Fact]
    public async Task Handle_ForUnknownUserId_ThrowsAuthenticationFailedException()
    {
        var store = new FakeUserStore();
        var handler = CreateHandler(store);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.Handle(new UpdatePhoneNumberCommand(Guid.NewGuid(), "+373 69 123 456"), CancellationToken.None));
    }
}
