using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Properties.ReinstateListing;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class ReinstateListingHandlerTests
{
    private static Property AddSuspendedProperty(ImovaDbContext dbContext, Guid ownerId)
    {
        var property = Property.Create(
            ownerId, "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        property.SubmitForReview();
        property.Approve();
        property.Suspend("Reported as a duplicate.");
        dbContext.Properties.Add(property);
        return property;
    }

    [Fact]
    public async Task Handle_ByAdmin_FromSuspended_Reinstates()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddSuspendedProperty(dbContext, Guid.NewGuid());
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new ReinstateListingHandler(dbContext);
        var result = await handler.Handle(new ReinstateListingCommand(property.Id, true), CancellationToken.None);

        Assert.Equal("Published", result!.Status);
        Assert.Null(result.SuspensionReason);
    }

    [Fact]
    public async Task Handle_WhenNotSuspended_ThrowsValidationException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = Property.Create(
            Guid.NewGuid(), "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        property.SubmitForReview();
        property.Approve();
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new ReinstateListingHandler(dbContext);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new ReinstateListingCommand(property.Id, true), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ByNonAdmin_ThrowsForbiddenAccessException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddSuspendedProperty(dbContext, Guid.NewGuid());
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new ReinstateListingHandler(dbContext);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new ReinstateListingCommand(property.Id, false), CancellationToken.None));
        Assert.Equal(PropertyStatus.Suspended, property.Status);
    }

    [Fact]
    public async Task Handle_ForUnknownPropertyId_ReturnsNull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new ReinstateListingHandler(dbContext);

        var result = await handler.Handle(new ReinstateListingCommand(Guid.NewGuid(), true), CancellationToken.None);

        Assert.Null(result);
    }
}
