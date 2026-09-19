using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Properties.SuspendListing;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class SuspendListingHandlerTests
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
    public async Task Handle_ByAdmin_FromPublished_SuspendsAndStoresReason()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddPublishedProperty(dbContext, Guid.NewGuid());
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new SuspendListingHandler(dbContext);
        var result = await handler.Handle(
            new SuspendListingCommand(property.Id, true, "Reported as a duplicate."), CancellationToken.None);

        Assert.Equal("Suspended", result!.Status);
        Assert.Equal("Reported as a duplicate.", result.SuspensionReason);
    }

    [Fact]
    public async Task Handle_WhenNotPublished_ThrowsValidationException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = Property.Create(
            Guid.NewGuid(), "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new SuspendListingHandler(dbContext);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new SuspendListingCommand(property.Id, true, "Some reason."), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ByNonAdmin_ThrowsForbiddenAccessException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddPublishedProperty(dbContext, Guid.NewGuid());
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new SuspendListingHandler(dbContext);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new SuspendListingCommand(property.Id, false, "Some reason."), CancellationToken.None));
        Assert.Equal(PropertyStatus.Published, property.Status);
    }

    [Fact]
    public async Task Handle_ForUnknownPropertyId_ReturnsNull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new SuspendListingHandler(dbContext);

        var result = await handler.Handle(
            new SuspendListingCommand(Guid.NewGuid(), true, "Some reason."), CancellationToken.None);

        Assert.Null(result);
    }
}
