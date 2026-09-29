using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Account;
using Imova.Application.Features.Account.ConfirmAccountDeletion;
using Imova.Application.Features.Account.DeleteAccount;
using Imova.Application.Features.Account.RequestAccountDeletionLink;
using Imova.Application.Features.Auth;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Account;

// Proving it's really the owner before an account is erased: the password (with the sign-in
// lockout), or the emailed link for an account without one.
public class AccountDeletionHandlersTests
{
    private const string Password = "Secret123!";

    private readonly FakeUserStore _store = new();
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly FakeEmailSender _email = new();
    private readonly AppOptions _app = new() { WebBaseUrl = "https://imova.md" };
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountDeletionHandlersTests()
    {
        _userManager = TestUserManagerFactory.Create(_store);
    }

    private AccountDeletionEmails Emails() => new(_userManager, _email, _app);

    private AccountDeletion Deletion() =>
        new(_db, new FakeBlobStorageService(), Emails(), NullLogger<AccountDeletion>.Instance);

    // The same account in the Identity store (password, lockout) and in the database (everything else).
    private ApplicationUser Seed(bool withPassword = true)
    {
        var user = withPassword
            ? _store.SeedUserWithPassword($"{Guid.NewGuid():N}@example.com", Password)
            : _store.SeedUser($"{Guid.NewGuid():N}@example.com", emailConfirmed: true);
        ListingTestData.AddUser(_db, user.Id);
        _db.SaveChanges();
        return user;
    }

    private Task DeleteAsync(Guid userId, string? password) =>
        new DeleteAccountHandler(_userManager, Deletion()).Handle(new DeleteAccountCommand(userId, password), CancellationToken.None);

    private Task RequestLinkAsync(Guid userId, AuthEmailThrottle? throttle = null) =>
        new RequestAccountDeletionLinkHandler(_userManager, Emails(), throttle ?? new AuthEmailThrottle(TimeProvider.System))
            .Handle(new RequestAccountDeletionLinkCommand(userId), CancellationToken.None);

    private Task ConfirmAsync(Guid userId, string token) =>
        new ConfirmAccountDeletionHandler(_userManager, Deletion())
            .Handle(new ConfirmAccountDeletionCommand(userId, token), CancellationToken.None);

    private Task<bool> ExistsAsync(Guid userId) => _db.Users.AnyAsync(u => u.Id == userId);

    [Fact]
    public async Task Delete_WithTheRightPassword_DeletesTheAccount()
    {
        var user = Seed();

        await DeleteAsync(user.Id, Password);

        Assert.False(await ExistsAsync(user.Id));
    }

    [Fact]
    public async Task Delete_WithoutAPassword_IsRefused()
    {
        var user = Seed();

        var error = await Assert.ThrowsAsync<ValidationException>(() => DeleteAsync(user.Id, null));

        Assert.Equal(ErrorCodes.AccountPasswordRequired, Assert.Single(error.Errors).ErrorCode);
        Assert.True(await ExistsAsync(user.Id));
    }

    [Fact]
    public async Task Delete_WithTheWrongPassword_IsRefused_AndCountsAsAFailedAttempt()
    {
        var user = Seed();

        var error = await Assert.ThrowsAsync<ValidationException>(() => DeleteAsync(user.Id, "Wrong123!"));

        var failure = Assert.Single(error.Errors);
        Assert.Equal(nameof(DeleteAccountCommand.Password), failure.PropertyName);
        Assert.Equal(ErrorCodes.AccountWrongPassword, failure.ErrorCode);
        Assert.Equal(1, await _userManager.GetAccessFailedCountAsync(user));
        Assert.True(await ExistsAsync(user.Id));
    }

    [Fact]
    public async Task Delete_AfterTooManyWrongPasswords_LocksOut_EvenForTheRightOne()
    {
        var user = Seed();
        for (var i = 0; i < 4; i++)
        {
            await Assert.ThrowsAsync<ValidationException>(() => DeleteAsync(user.Id, "Wrong123!"));
        }

        var locked = await Assert.ThrowsAsync<TooManyRequestsException>(() => DeleteAsync(user.Id, "Wrong123!"));
        Assert.Equal(ErrorCodes.LockedOut, locked.Code);

        await Assert.ThrowsAsync<TooManyRequestsException>(() => DeleteAsync(user.Id, Password));
        Assert.True(await ExistsAsync(user.Id));
    }

    [Fact]
    public async Task Delete_ForAnAccountWithoutAPassword_PointsToTheEmailedLink()
    {
        var user = Seed(withPassword: false);

        var error = await Assert.ThrowsAsync<ValidationException>(() => DeleteAsync(user.Id, "anything"));

        Assert.Equal(ErrorCodes.AccountNoPassword, Assert.Single(error.Errors).ErrorCode);
        Assert.True(await ExistsAsync(user.Id));
    }

    [Fact]
    public async Task RequestLink_EmailsALinkToTheConfirmationPage()
    {
        var user = Seed(withPassword: false);

        await RequestLinkAsync(user.Id);

        var email = Assert.Single(_email.Sent);
        Assert.Equal(user.Email, email.To);
        var expectedToken = AccountTokens.Encode(FakeTokenProvider.TokenFor(AccountDeletion.TokenPurpose, user));
        Assert.Contains($"https://imova.md/delete-account?userId={user.Id}&token={expectedToken}", email.TextBody);
        Assert.Contains("24 de ore", email.TextBody);
        Assert.True(await ExistsAsync(user.Id));
    }

    [Fact]
    public async Task RequestLink_TwiceInAMinute_IsThrottled()
    {
        var user = Seed(withPassword: false);
        var throttle = new AuthEmailThrottle(TimeProvider.System);
        await RequestLinkAsync(user.Id, throttle);

        var error = await Assert.ThrowsAsync<TooManyRequestsException>(() => RequestLinkAsync(user.Id, throttle));

        Assert.Equal(ErrorCodes.EmailThrottled, error.Code);
        Assert.Single(_email.Sent);
    }

    [Fact]
    public async Task Confirm_WithTheEmailedToken_DeletesTheAccount()
    {
        var user = Seed(withPassword: false);

        await ConfirmAsync(user.Id, AccountTokens.Encode(FakeTokenProvider.TokenFor(AccountDeletion.TokenPurpose, user)));

        Assert.False(await ExistsAsync(user.Id));
    }

    [Theory]
    [InlineData("EmailConfirmation")]
    [InlineData("ResetPassword")]
    public async Task Confirm_WithATokenMadeForSomethingElse_IsAnInvalidLink(string otherPurpose)
    {
        var user = Seed(withPassword: false);

        var error = await Assert.ThrowsAsync<ValidationException>(
            () => ConfirmAsync(user.Id, AccountTokens.Encode(FakeTokenProvider.TokenFor(otherPurpose, user))));

        Assert.Equal(ErrorCodes.InvalidLink, Assert.Single(error.Errors).ErrorCode);
        Assert.True(await ExistsAsync(user.Id));
    }

    [Fact]
    public async Task Confirm_WithAnotherAccountsToken_IsAnInvalidLink()
    {
        var user = Seed(withPassword: false);
        var other = Seed(withPassword: false);

        await Assert.ThrowsAsync<ValidationException>(
            () => ConfirmAsync(user.Id, AccountTokens.Encode(FakeTokenProvider.TokenFor(AccountDeletion.TokenPurpose, other))));

        Assert.True(await ExistsAsync(user.Id));
        Assert.True(await ExistsAsync(other.Id));
    }

    [Fact]
    public async Task Confirm_ForAnUnknownAccount_IsAnInvalidLink()
    {
        var error = await Assert.ThrowsAsync<ValidationException>(() => ConfirmAsync(Guid.NewGuid(), AccountTokens.Encode("x")));

        Assert.Equal(ErrorCodes.InvalidLink, Assert.Single(error.Errors).ErrorCode);
    }

    [Fact]
    public void Validators_RejectAnEmptyTokenAndAnOverlongPassword()
    {
        Assert.False(new ConfirmAccountDeletionValidator().Validate(new ConfirmAccountDeletionCommand(Guid.NewGuid(), "")).IsValid);
        Assert.False(new ConfirmAccountDeletionValidator().Validate(new ConfirmAccountDeletionCommand(Guid.Empty, "t")).IsValid);
        Assert.False(new DeleteAccountValidator().Validate(new DeleteAccountCommand(Guid.NewGuid(), new string('x', 201))).IsValid);
        Assert.True(new DeleteAccountValidator().Validate(new DeleteAccountCommand(Guid.NewGuid(), Password)).IsValid);
    }
}
