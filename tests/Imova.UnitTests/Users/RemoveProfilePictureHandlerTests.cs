using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Users.RemoveProfilePicture;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Users;

public class RemoveProfilePictureHandlerTests
{
    private static RemoveProfilePictureHandler CreateHandler(FakeUserStore store, FakeBlobStorageService blobStorage) =>
        new(TestUserManagerFactory.Create(store), blobStorage);

    [Fact]
    public async Task Handle_WithExistingPicture_DeletesBlobAndClearsProfilePictureUrl()
    {
        var store = new FakeUserStore();
        var user = store.SeedUser("user@example.com", emailConfirmed: true);
        var blobStorage = new FakeBlobStorageService();
        user.ProfilePictureUrl = blobStorage.GetPublicUrl(blobStorage.GenerateProfilePictureBlobName(user.Id, ".jpg"));
        var blobName = blobStorage.TryGetBlobNameFromUrl(user.ProfilePictureUrl)!;
        var handler = CreateHandler(store, blobStorage);

        var result = await handler.Handle(new RemoveProfilePictureCommand(user.Id), CancellationToken.None);

        Assert.Null(user.ProfilePictureUrl);
        Assert.Null(result.ProfilePictureUrl);
        Assert.Contains(blobName, blobStorage.DeletedBlobNames);
    }

    [Fact]
    public async Task Handle_WithNoExistingPicture_IsANoOp()
    {
        var store = new FakeUserStore();
        var user = store.SeedUser("user@example.com", emailConfirmed: true);
        var blobStorage = new FakeBlobStorageService();
        var handler = CreateHandler(store, blobStorage);

        var result = await handler.Handle(new RemoveProfilePictureCommand(user.Id), CancellationToken.None);

        Assert.Null(result.ProfilePictureUrl);
        Assert.Empty(blobStorage.DeletedBlobNames);
    }

    [Fact]
    public async Task Handle_ForUnknownUserId_ThrowsAuthenticationFailedException()
    {
        var store = new FakeUserStore();
        var blobStorage = new FakeBlobStorageService();
        var handler = CreateHandler(store, blobStorage);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.Handle(new RemoveProfilePictureCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
