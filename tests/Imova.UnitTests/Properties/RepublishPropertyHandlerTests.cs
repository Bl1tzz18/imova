using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Properties.RepublishProperty;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class RepublishPropertyHandlerTests
{
    private static Property AddArchivedProperty(ImovaDbContext dbContext, Guid ownerId)
    {
        var property = Property.Create(
            ownerId, "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        property.SubmitForReview();
        property.Approve();
        property.Archive();
        dbContext.Properties.Add(property);
        return property;
    }

    [Fact]
    public async Task Handle_ByOwner_RepublishesArchivedListing()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddArchivedProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new RepublishPropertyHandler(dbContext);
        var result = await handler.Handle(new RepublishPropertyCommand(property.Id, ownerId, false), CancellationToken.None);

        Assert.Equal("Published", result!.Status);
    }

    [Fact]
    public async Task Handle_WhenNotArchived_ThrowsValidationException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var draft = Property.Create(
            ownerId, "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        dbContext.Properties.Add(draft);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new RepublishPropertyHandler(dbContext);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new RepublishPropertyCommand(draft.Id, ownerId, false), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ByNonOwnerNonAdmin_ThrowsForbiddenAccessException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddArchivedProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new RepublishPropertyHandler(dbContext);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new RepublishPropertyCommand(property.Id, Guid.NewGuid(), false), CancellationToken.None));
        Assert.Equal(PropertyStatus.Archived, property.Status);
    }

    [Fact]
    public async Task Handle_ByAdminNonOwner_Succeeds()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddArchivedProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new RepublishPropertyHandler(dbContext);
        var result = await handler.Handle(new RepublishPropertyCommand(property.Id, Guid.NewGuid(), true), CancellationToken.None);

        Assert.Equal("Published", result!.Status);
    }

    [Fact]
    public async Task Handle_ForUnknownPropertyId_ReturnsNull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new RepublishPropertyHandler(dbContext);

        var result = await handler.Handle(new RepublishPropertyCommand(Guid.NewGuid(), Guid.NewGuid(), false), CancellationToken.None);

        Assert.Null(result);
    }
}
