using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Properties.MarkAsRented;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class MarkAsRentedHandlerTests
{
    private static Property AddPublishedProperty(ImovaDbContext dbContext, Guid ownerId, ListingType listingType = ListingType.Rent)
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
    public async Task Handle_ByOwner_FromPublishedRentalListing_MarksAsRented()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddPublishedProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkAsRentedHandler(dbContext);
        var result = await handler.Handle(new MarkAsRentedCommand(property.Id, ownerId, false), CancellationToken.None);

        Assert.Equal("Rented", result!.Status);
    }

    [Fact]
    public async Task Handle_WhenNotPublished_ThrowsValidationException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = Property.Create(
            ownerId, "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkAsRentedHandler(dbContext);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new MarkAsRentedCommand(property.Id, ownerId, false), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForSaleListing_ThrowsValidationException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddPublishedProperty(dbContext, ownerId, ListingType.Sale);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkAsRentedHandler(dbContext);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new MarkAsRentedCommand(property.Id, ownerId, false), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ByNonOwnerNonAdmin_ThrowsForbiddenAccessException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddPublishedProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkAsRentedHandler(dbContext);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new MarkAsRentedCommand(property.Id, Guid.NewGuid(), false), CancellationToken.None));
        Assert.Equal(PropertyStatus.Published, property.Status);
    }

    [Fact]
    public async Task Handle_ByAdminNonOwner_Succeeds()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddPublishedProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new MarkAsRentedHandler(dbContext);
        var result = await handler.Handle(new MarkAsRentedCommand(property.Id, Guid.NewGuid(), true), CancellationToken.None);

        Assert.Equal("Rented", result!.Status);
    }

    [Fact]
    public async Task Handle_ForUnknownPropertyId_ReturnsNull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new MarkAsRentedHandler(dbContext);

        var result = await handler.Handle(new MarkAsRentedCommand(Guid.NewGuid(), Guid.NewGuid(), false), CancellationToken.None);

        Assert.Null(result);
    }
}
