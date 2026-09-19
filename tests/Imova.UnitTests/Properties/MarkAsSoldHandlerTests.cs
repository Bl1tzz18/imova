using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Properties.MarkAsSold;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class MarkAsSoldHandlerTests
{
    private static Property AddPublishedProperty(ImovaDbContext dbContext, Guid ownerId, ListingType listingType = ListingType.Sale)
    {
        var property = Property.Create(
            ownerId, "Titlu", "Descriere", PropertyType.Apartment, listingType, 550m, "EUR",
            54m, 2m, null, 3, 9);
        property.SubmitForReview();
        property.Approve();
        dbContext.Properties.Add(property);
        return property;
    }

    [Fact]
    public async Task Handle_ByOwner_FromPublishedSaleListing_MarksAsSold()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddPublishedProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkAsSoldHandler(dbContext);
        var result = await handler.Handle(new MarkAsSoldCommand(property.Id, ownerId, false), CancellationToken.None);

        Assert.Equal("Sold", result!.Status);
    }

    [Fact]
    public async Task Handle_WhenNotPublished_ThrowsValidationException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = Property.Create(
            ownerId, "Titlu", "Descriere", PropertyType.Apartment, ListingType.Sale, 550m, "EUR",
            54m, 2m, null, 3, 9);
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkAsSoldHandler(dbContext);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new MarkAsSoldCommand(property.Id, ownerId, false), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForRentalListing_ThrowsValidationException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddPublishedProperty(dbContext, ownerId, ListingType.Rent);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkAsSoldHandler(dbContext);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new MarkAsSoldCommand(property.Id, ownerId, false), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ByNonOwnerNonAdmin_ThrowsForbiddenAccessException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddPublishedProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkAsSoldHandler(dbContext);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new MarkAsSoldCommand(property.Id, Guid.NewGuid(), false), CancellationToken.None));
        Assert.Equal(PropertyStatus.Published, property.Status);
    }

    [Fact]
    public async Task Handle_ByAdminNonOwner_Succeeds()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddPublishedProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkAsSoldHandler(dbContext);
        var result = await handler.Handle(new MarkAsSoldCommand(property.Id, Guid.NewGuid(), true), CancellationToken.None);

        Assert.Equal("Sold", result!.Status);
    }

    [Fact]
    public async Task Handle_ForUnknownPropertyId_ReturnsNull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MarkAsSoldHandler(dbContext);

        var result = await handler.Handle(new MarkAsSoldCommand(Guid.NewGuid(), Guid.NewGuid(), false), CancellationToken.None);

        Assert.Null(result);
    }
}
