using FluentValidation;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Auth;
using Imova.Application.Features.Auth.ResetPassword;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.AspNetCore.Identity;

namespace Imova.UnitTests.Auth;

public class ResetPasswordHandlerTests
{
    private readonly FakeUserStore _store = new();
    private readonly ImovaDbContext _dbContext = TestDbContextFactory.Create();
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordHandlerTests()
    {
        _userManager = TestUserManagerFactory.Create(_store);
    }

    private Task ResetAsync(string email, string token, string newPassword = "NewPassword1") =>
        new ResetPasswordHandler(_userManager, _dbContext)
            .Handle(new ResetPasswordCommand(email, token, newPassword), CancellationToken.None);

    private static string LinkToken(ApplicationUser user) =>
        AccountTokens.Encode(FakeTokenProvider.TokenFor("ResetPassword", user));

    [Fact]
    public async Task Handle_WithTheEmailedToken_ChangesThePassword()
    {
        var user = _store.SeedUserWithPassword("ana@example.com", "OldPassword1");

        await ResetAsync("ana@example.com", LinkToken(user));

        Assert.True(await _userManager.CheckPasswordAsync(user, "NewPassword1"));
        Assert.False(await _userManager.CheckPasswordAsync(user, "OldPassword1"));
    }

    [Fact]
    public async Task Handle_ForAGoogleOnlyAccount_SetsItsFirstPassword()
    {
        var user = _store.SeedUser("google@example.com", emailConfirmed: true);

        await ResetAsync("google@example.com", LinkToken(user));

        Assert.True(await _userManager.CheckPasswordAsync(user, "NewPassword1"));
    }

    [Theory]
    [InlineData("wrong-token")]
    [InlineData("!!not-base64url!!")]
    public async Task Handle_WithABadToken_FailsAsAnInvalidLink(string token)
    {
        var user = _store.SeedUserWithPassword("ana@example.com", "OldPassword1");

        var error = await Assert.ThrowsAsync<ValidationException>(() =>
            ResetAsync("ana@example.com", token == "wrong-token" ? AccountTokens.Encode(token) : token));

        Assert.Contains(error.Errors, e => e.PropertyName == "Token" && e.ErrorMessage == ResetPasswordHandler.InvalidLink);
        Assert.True(await _userManager.CheckPasswordAsync(user, "OldPassword1"));
    }

    [Fact]
    public async Task Handle_ForAnUnknownEmail_FailsTheSameWayAsABadToken()
    {
        var error = await Assert.ThrowsAsync<ValidationException>(() => ResetAsync("nobody@example.com", AccountTokens.Encode("x")));

        Assert.Contains(error.Errors, e => e.ErrorMessage == ResetPasswordHandler.InvalidLink);
    }

    [Fact]
    public async Task Handle_LiftsASignInLockout()
    {
        var user = _store.SeedUserWithPassword("ana@example.com", "OldPassword1");
        user.AccessFailedCount = 3;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10);

        await ResetAsync("ana@example.com", LinkToken(user));

        Assert.False(await _userManager.IsLockedOutAsync(user));
        Assert.Equal(0, user.AccessFailedCount);
    }

    [Fact]
    public async Task Handle_ForAnUnconfirmedEmail_ConfirmsItAndSubmitsTheWaitingDrafts()
    {
        // The reset link reached the inbox — that proves the address as well as a confirmation link.
        var user = _store.SeedUser("ana@example.com", emailConfirmed: false);
        var publisher = ListingTestData.AddIndividualPublisher(_dbContext, user.Id);
        var draft = ListingTestData.AddListing(_dbContext, publisher.Id);
        await _dbContext.SaveChangesAsync();

        await ResetAsync("ana@example.com", LinkToken(user));

        Assert.True(user.EmailConfirmed);
        Assert.Equal(ListingStatus.PendingReview, draft.Status);
    }
}
