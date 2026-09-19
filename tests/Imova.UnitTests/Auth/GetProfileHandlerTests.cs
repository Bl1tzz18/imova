using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Auth.GetProfile;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Auth;

public class GetProfileHandlerTests
{
    private static GetProfileHandler CreateHandler(FakeUserStore store) =>
        new(TestUserManagerFactory.Create(store));

    [Fact]
    public async Task Handle_ForExistingUserWithPassword_ReturnsProfileWithHasPasswordTrue()
    {
        var store = new FakeUserStore();
        var user = store.SeedUserWithPassword("user@example.com", "SomePassword1");
        var handler = CreateHandler(store);

        var result = await handler.Handle(new GetProfileQuery(user.Id), CancellationToken.None);

        Assert.Equal(user.Id, result.Id);
        Assert.Equal("user@example.com", result.Email);
        Assert.True(result.HasPassword);
    }

    [Fact]
    public async Task Handle_ForGoogleOnlyUser_ReturnsProfileWithHasPasswordFalse()
    {
        var store = new FakeUserStore();
        var user = store.SeedUser("google-only@example.com", emailConfirmed: true);
        var handler = CreateHandler(store);

        var result = await handler.Handle(new GetProfileQuery(user.Id), CancellationToken.None);

        Assert.False(result.HasPassword);
    }

    [Fact]
    public async Task Handle_ForUnknownUserId_ThrowsAuthenticationFailedException()
    {
        var store = new FakeUserStore();
        var handler = CreateHandler(store);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.Handle(new GetProfileQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
