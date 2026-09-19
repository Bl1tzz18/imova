using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Properties.RejectListing;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class RejectListingHandlerTests
{
    private static Property AddPendingReviewProperty(ImovaDbContext dbContext, Guid ownerId)
    {
        var property = Property.Create(
            ownerId, "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        property.SubmitForReview();
        dbContext.Properties.Add(property);
        return property;
    }

    [Fact]
    public async Task Handle_ByAdmin_FromPendingReview_RejectsAndStoresReason()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddPendingReviewProperty(dbContext, Guid.NewGuid());
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new RejectListingHandler(dbContext);
        var result = await handler.Handle(
            new RejectListingCommand(property.Id, true, "Missing required photos."), CancellationToken.None);

        Assert.Equal("Rejected", result!.Status);
        Assert.Equal("Missing required photos.", result.RejectionReason);
    }

    [Fact]
    public async Task Handle_WhenNotPendingReview_ThrowsValidationException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = Property.Create(
            Guid.NewGuid(), "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new RejectListingHandler(dbContext);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new RejectListingCommand(property.Id, true, "Some reason."), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ByNonAdmin_ThrowsForbiddenAccessException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddPendingReviewProperty(dbContext, Guid.NewGuid());
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new RejectListingHandler(dbContext);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new RejectListingCommand(property.Id, false, "Some reason."), CancellationToken.None));
        Assert.Equal(PropertyStatus.PendingReview, property.Status);
    }

    [Fact]
    public async Task Handle_ForUnknownPropertyId_ReturnsNull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new RejectListingHandler(dbContext);

        var result = await handler.Handle(
            new RejectListingCommand(Guid.NewGuid(), true, "Some reason."), CancellationToken.None);

        Assert.Null(result);
    }
}
