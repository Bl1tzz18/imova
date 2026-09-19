using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Users.UploadProfilePicture;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Users;

public class UploadProfilePictureHandlerTests
{
    // Minimal valid PNG signature — see ImageSignature.DetectContentType.
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static UploadProfilePictureHandler CreateHandler(FakeUserStore store, FakeBlobStorageService blobStorage) =>
        new(TestUserManagerFactory.Create(store), blobStorage);

    [Fact]
    public async Task Handle_WithRecognizedImage_UploadsAndSetsProfilePictureUrl()
    {
        var store = new FakeUserStore();
        var user = store.SeedUser("user@example.com", emailConfirmed: true);
        var blobStorage = new FakeBlobStorageService();
        var handler = CreateHandler(store, blobStorage);

        var result = await handler.Handle(new UploadProfilePictureCommand(user.Id, PngBytes), CancellationToken.None);

        Assert.True(blobStorage.UploadCalled);
        Assert.Equal("image/png", blobStorage.UploadedContentType);
        Assert.NotNull(result.ProfilePictureUrl);
        Assert.Equal(user.ProfilePictureUrl, result.ProfilePictureUrl);
    }

    [Fact]
    public async Task Handle_WithUnrecognizedFileFormat_ThrowsValidationException()
    {
        var store = new FakeUserStore();
        var user = store.SeedUser("user@example.com", emailConfirmed: true);
        var blobStorage = new FakeBlobStorageService();
        var handler = CreateHandler(store, blobStorage);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new UploadProfilePictureCommand(user.Id, [0x00, 0x01, 0x02]), CancellationToken.None));

        Assert.False(blobStorage.UploadCalled);
    }

    [Fact]
    public async Task Handle_WithExistingPicture_DeletesThePreviousBlobAfterUploadingTheNewOne()
    {
        var store = new FakeUserStore();
        var user = store.SeedUser("user@example.com", emailConfirmed: true);
        var blobStorage = new FakeBlobStorageService();
        user.ProfilePictureUrl = blobStorage.GetPublicUrl(blobStorage.GenerateProfilePictureBlobName(user.Id, ".jpg"));
        var previousBlobName = blobStorage.TryGetBlobNameFromUrl(user.ProfilePictureUrl)!;
        var handler = CreateHandler(store, blobStorage);

        await handler.Handle(new UploadProfilePictureCommand(user.Id, PngBytes), CancellationToken.None);

        Assert.Contains(previousBlobName, blobStorage.DeletedBlobNames);
    }

    [Fact]
    public async Task Handle_ForUnknownUserId_ThrowsAuthenticationFailedException()
    {
        var store = new FakeUserStore();
        var blobStorage = new FakeBlobStorageService();
        var handler = CreateHandler(store, blobStorage);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.Handle(new UploadProfilePictureCommand(Guid.NewGuid(), PngBytes), CancellationToken.None));
    }
}
