using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Properties.ArchiveProperty;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class ArchivePropertyHandlerTests
{
    private static Property AddPublishedProperty(ImovaDbContext dbContext, Guid ownerId)
    {
        var property = Property.Create(
            ownerId, "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        property.SubmitForReview();
        property.Approve();
        dbContext.Properties.Add(property);
        return property;
    }

    [Fact]
    public async Task Handle_ByOwner_ArchivesPublishedListing()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddPublishedProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new ArchivePropertyHandler(dbContext);
        var result = await handler.Handle(new ArchivePropertyCommand(property.Id, ownerId, false), CancellationToken.None);

        Assert.Equal("Archived", result!.Status);
    }

    [Fact]
    public async Task Handle_WhenNotPublished_ThrowsValidationException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var draft = Property.Create(
            ownerId, "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        dbContext.Properties.Add(draft);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new ArchivePropertyHandler(dbContext);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new ArchivePropertyCommand(draft.Id, ownerId, false), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ByNonOwnerNonAdmin_ThrowsForbiddenAccessException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddPublishedProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new ArchivePropertyHandler(dbContext);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new ArchivePropertyCommand(property.Id, Guid.NewGuid(), false), CancellationToken.None));
        Assert.Equal(PropertyStatus.Published, property.Status);
    }

    [Fact]
    public async Task Handle_ByAdminNonOwner_Succeeds()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddPublishedProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new ArchivePropertyHandler(dbContext);
        var result = await handler.Handle(new ArchivePropertyCommand(property.Id, Guid.NewGuid(), true), CancellationToken.None);

        Assert.Equal("Archived", result!.Status);
    }

    [Fact]
    public async Task Handle_ByOwner_ArchivesRentedListing()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddPublishedProperty(dbContext, ownerId);
        property.MarkAsRented();
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new ArchivePropertyHandler(dbContext);
        var result = await handler.Handle(new ArchivePropertyCommand(property.Id, ownerId, false), CancellationToken.None);

        Assert.Equal("Archived", result!.Status);
    }

    [Fact]
    public async Task Handle_ForUnknownPropertyId_ReturnsNull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new ArchivePropertyHandler(dbContext);

        var result = await handler.Handle(new ArchivePropertyCommand(Guid.NewGuid(), Guid.NewGuid(), false), CancellationToken.None);

        Assert.Null(result);
    }
}
