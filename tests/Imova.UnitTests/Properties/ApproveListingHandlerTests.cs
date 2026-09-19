using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Properties.ApproveListing;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class ApproveListingHandlerTests
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
    public async Task Handle_ByAdmin_FromPendingReview_ApprovesAndPublishes()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddPendingReviewProperty(dbContext, Guid.NewGuid());
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new ApproveListingHandler(dbContext);
        var result = await handler.Handle(new ApproveListingCommand(property.Id, true), CancellationToken.None);

        Assert.Equal("Published", result!.Status);
        Assert.NotNull(result.PublishedAt);
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

        var handler = new ApproveListingHandler(dbContext);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new ApproveListingCommand(property.Id, true), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ByNonAdmin_ThrowsForbiddenAccessException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddPendingReviewProperty(dbContext, Guid.NewGuid());
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new ApproveListingHandler(dbContext);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new ApproveListingCommand(property.Id, false), CancellationToken.None));
        Assert.Equal(PropertyStatus.PendingReview, property.Status);
    }

    [Fact]
    public async Task Handle_ForUnknownPropertyId_ReturnsNull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new ApproveListingHandler(dbContext);

        var result = await handler.Handle(new ApproveListingCommand(Guid.NewGuid(), true), CancellationToken.None);

        Assert.Null(result);
    }
}
