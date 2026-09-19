using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Media.DeleteMedia;
using Imova.Domain.Media;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Media;

public class DeleteMediaHandlerTests
{
    private static Property AddProperty(ImovaDbContext dbContext, Guid ownerId)
    {
        var property = Property.Create(
            ownerId, "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        dbContext.Properties.Add(property);
        return property;
    }

    [Fact]
    public async Task Handle_ByOwner_RemovesMediaRowAndDeletesBlobAndReturnsTrue()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        var media = PropertyMedia.Create(property.Id, $"{property.Id}/photo.jpg", "image/jpeg", 1024);
        dbContext.PropertyMedias.Add(media);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var blobStorage = new FakeBlobStorageService();
        var handler = new DeleteMediaHandler(dbContext, blobStorage);

        var result = await handler.Handle(new DeleteMediaCommand(property.Id, media.Id, ownerId, false), CancellationToken.None);

        Assert.True(result);
        Assert.Empty(dbContext.PropertyMedias);
        Assert.Contains(media.BlobName, blobStorage.DeletedBlobNames);
    }

    [Fact]
    public async Task Handle_ByNonOwnerNonAdmin_ThrowsForbiddenAccessExceptionAndLeavesMediaIntact()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        var media = PropertyMedia.Create(property.Id, $"{property.Id}/photo.jpg", "image/jpeg", 1024);
        dbContext.PropertyMedias.Add(media);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteMediaHandler(dbContext, new FakeBlobStorageService());
        var otherUserId = Guid.NewGuid();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new DeleteMediaCommand(property.Id, media.Id, otherUserId, false), CancellationToken.None));

        Assert.Single(dbContext.PropertyMedias);
    }

    [Fact]
    public async Task Handle_ByAdminNonOwner_Succeeds()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        var media = PropertyMedia.Create(property.Id, $"{property.Id}/photo.jpg", "image/jpeg", 1024);
        dbContext.PropertyMedias.Add(media);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteMediaHandler(dbContext, new FakeBlobStorageService());
        var adminId = Guid.NewGuid();

        var result = await handler.Handle(new DeleteMediaCommand(property.Id, media.Id, adminId, true), CancellationToken.None);

        Assert.True(result);
        Assert.Empty(dbContext.PropertyMedias);
    }

    [Fact]
    public async Task Handle_ForUnknownMediaId_ReturnsFalse()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteMediaHandler(dbContext, new FakeBlobStorageService());

        var result = await handler.Handle(
            new DeleteMediaCommand(property.Id, Guid.NewGuid(), ownerId, false), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_ForMediaBelongingToDifferentProperty_ReturnsFalse()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddProperty(dbContext, ownerId);
        var otherProperty = AddProperty(dbContext, ownerId);
        var media = PropertyMedia.Create(otherProperty.Id, $"{otherProperty.Id}/photo.jpg", "image/jpeg", 1024);
        dbContext.PropertyMedias.Add(media);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteMediaHandler(dbContext, new FakeBlobStorageService());

        var result = await handler.Handle(
            new DeleteMediaCommand(property.Id, media.Id, ownerId, false), CancellationToken.None);

        Assert.False(result);
        Assert.Single(dbContext.PropertyMedias);
    }
}
