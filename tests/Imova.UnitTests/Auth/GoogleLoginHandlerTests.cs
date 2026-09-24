using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Auth.GoogleLogin;
using Imova.Domain.Publishers;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Auth;

public class GoogleLoginHandlerTests
{
    private static GoogleLoginHandler CreateHandler(
        FakeUserStore store,
        FakeGoogleTokenValidator googleTokenValidator,
        FakeExternalImageFetcher? imageFetcher = null,
        FakeBlobStorageService? blobStorageService = null,
        ImovaDbContext? dbContext = null) =>
        new(
            googleTokenValidator,
            TestUserManagerFactory.Create(store),
            new FakeJwtTokenGenerator(),
            imageFetcher ?? new FakeExternalImageFetcher(),
            blobStorageService ?? new FakeBlobStorageService(),
            dbContext ?? TestDbContextFactory.Create(),
            NullLogger<GoogleLoginHandler>.Instance);

    [Fact]
    public async Task Handle_WithNoExistingAccount_CreatesAnIndividualPublisherForTheNewUser()
    {
        var store = new FakeUserStore();
        var validator = new FakeGoogleTokenValidator
        {
            UserToReturn = new GoogleUserInfo("new.user@example.com", "New User", null),
        };
        await using var dbContext = TestDbContextFactory.Create();
        var handler = CreateHandler(store, validator, dbContext: dbContext);

        await handler.Handle(new GoogleLoginCommand("valid-id-token"), CancellationToken.None);

        var publisher = Assert.Single(dbContext.Publishers);
        Assert.Equal(Assert.Single(store.Users).Id, publisher.UserId);
        Assert.Equal(PublisherType.Individual, publisher.PublisherType);
        Assert.Equal("New User", publisher.DisplayName);
        Assert.Equal("new.user@example.com", publisher.Email);
        // Google accounts have no phone number until the user completes their profile.
        Assert.Null(publisher.Phone);
    }

    [Fact]
    public async Task Handle_WithExistingAccount_DoesNotCreateAnotherPublisher()
    {
        var store = new FakeUserStore();
        store.SeedUser("existing@example.com", emailConfirmed: true);
        var validator = new FakeGoogleTokenValidator
        {
            UserToReturn = new GoogleUserInfo("existing@example.com", "Existing", null),
        };
        await using var dbContext = TestDbContextFactory.Create();
        var handler = CreateHandler(store, validator, dbContext: dbContext);

        await handler.Handle(new GoogleLoginCommand("valid-id-token"), CancellationToken.None);

        Assert.Empty(dbContext.Publishers);
    }

    [Fact]
    public async Task Handle_WithNoExistingAccount_CreatesConfirmedUserAndAssignsUserRole()
    {
        var store = new FakeUserStore();
        var validator = new FakeGoogleTokenValidator
        {
            UserToReturn = new GoogleUserInfo("new.user@example.com", "New User", null),
        };
        var handler = CreateHandler(store, validator);

        var result = await handler.Handle(new GoogleLoginCommand("valid-id-token"), CancellationToken.None);

        var created = Assert.Single(store.Users);
        Assert.Equal("new.user@example.com", created.Email);
        Assert.True(created.EmailConfirmed);
        Assert.Equal("New User", created.DisplayName);
        Assert.Null(created.PasswordHash);
        Assert.Equal(created.Id, result.User.Id);
        // UserManager.AddToRoleAsync normalizes the role name (upper-invariant) before it reaches
        // the store, so compare case-insensitively rather than against the raw "User" constant.
        Assert.Contains(store.GetRolesSnapshot(created.Id), r => string.Equals(r, "User", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_WithExistingUnconfirmedAccount_ConfirmsEmail()
    {
        // Simulates an account originally created via email/password registration (which leaves
        // EmailConfirmed false, since there's no email verification flow yet) — Google re-verifying
        // the same email should flip it to confirmed. This is the fix under test.
        var store = new FakeUserStore();
        var existing = store.SeedUser("returning@example.com", emailConfirmed: false);
        var validator = new FakeGoogleTokenValidator
        {
            UserToReturn = new GoogleUserInfo("returning@example.com", "Returning User", null),
        };
        var handler = CreateHandler(store, validator);

        await handler.Handle(new GoogleLoginCommand("valid-id-token"), CancellationToken.None);

        Assert.True(existing.EmailConfirmed);
        Assert.Single(store.Users);
    }

    [Fact]
    public async Task Handle_WithExistingConfirmedAccount_DoesNotCreateDuplicateAndStaysConfirmed()
    {
        var store = new FakeUserStore();
        var existing = store.SeedUser("confirmed@example.com", emailConfirmed: true);
        var validator = new FakeGoogleTokenValidator
        {
            UserToReturn = new GoogleUserInfo("confirmed@example.com", "Confirmed User", null),
        };
        var handler = CreateHandler(store, validator);

        var result = await handler.Handle(new GoogleLoginCommand("valid-id-token"), CancellationToken.None);

        Assert.Single(store.Users);
        Assert.True(existing.EmailConfirmed);
        Assert.Equal(existing.Id, result.User.Id);
    }

    [Fact]
    public async Task Handle_WithInvalidGoogleToken_ThrowsAuthenticationFailedExceptionAndCreatesNoAccount()
    {
        var store = new FakeUserStore();
        var validator = new FakeGoogleTokenValidator { UserToReturn = null };
        var handler = CreateHandler(store, validator);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.Handle(new GoogleLoginCommand("bad-token"), CancellationToken.None));

        Assert.Empty(store.Users);
    }

    [Fact]
    public async Task Handle_WithNewAccountAndDownloadablePicture_UploadsAndSetsProfilePictureUrl()
    {
        var store = new FakeUserStore();
        var validator = new FakeGoogleTokenValidator
        {
            UserToReturn = new GoogleUserInfo("pic@example.com", "Pic User", "https://google.example/pic.png"),
        };
        var imageFetcher = new FakeExternalImageFetcher
        {
            // Minimal valid PNG signature — see ImageSignature.DetectContentType.
            BytesToReturn = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
        };
        var blobStorage = new FakeBlobStorageService();
        var handler = CreateHandler(store, validator, imageFetcher, blobStorage);

        var result = await handler.Handle(new GoogleLoginCommand("valid-id-token"), CancellationToken.None);

        Assert.True(blobStorage.UploadCalled);
        Assert.Equal("image/png", blobStorage.UploadedContentType);
        Assert.NotNull(result.User.ProfilePictureUrl);
    }

    [Fact]
    public async Task Handle_WithNewAccountAndPictureDownloadFailure_StillCreatesAccountWithoutPicture()
    {
        // TryDownloadAsync failing (network error, expired URL, ...) must never block account
        // creation — see GoogleLoginHandler.TrySyncProfilePictureAsync.
        var store = new FakeUserStore();
        var validator = new FakeGoogleTokenValidator
        {
            UserToReturn = new GoogleUserInfo("nopic@example.com", "No Pic User", "https://google.example/missing.png"),
        };
        var imageFetcher = new FakeExternalImageFetcher { BytesToReturn = null };
        var handler = CreateHandler(store, validator, imageFetcher);

        var result = await handler.Handle(new GoogleLoginCommand("valid-id-token"), CancellationToken.None);

        Assert.Single(store.Users);
        Assert.Null(result.User.ProfilePictureUrl);
    }
}
