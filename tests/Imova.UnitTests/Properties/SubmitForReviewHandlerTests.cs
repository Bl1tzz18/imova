using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Properties.SubmitForReview;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class SubmitForReviewHandlerTests
{
    private static Property AddDraftProperty(ImovaDbContext dbContext, Guid ownerId)
    {
        var property = Property.Create(
            ownerId, "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        dbContext.Properties.Add(property);
        return property;
    }

    [Fact]
    public async Task Handle_ByOwner_FromDraft_SubmitsForReview()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddDraftProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new SubmitForReviewHandler(dbContext);
        var result = await handler.Handle(new SubmitForReviewCommand(property.Id, ownerId, false), CancellationToken.None);

        Assert.Equal("PendingReview", result!.Status);
    }

    [Fact]
    public async Task Handle_ByOwner_FromRejected_ResubmitsForReview()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddDraftProperty(dbContext, ownerId);
        property.SubmitForReview();
        property.Reject("Missing photos.");
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new SubmitForReviewHandler(dbContext);
        var result = await handler.Handle(new SubmitForReviewCommand(property.Id, ownerId, false), CancellationToken.None);

        Assert.Equal("PendingReview", result!.Status);
        Assert.Null(result.RejectionReason);
    }

    [Fact]
    public async Task Handle_WhenAlreadyPendingReview_ThrowsValidationException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddDraftProperty(dbContext, ownerId);
        property.SubmitForReview();
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new SubmitForReviewHandler(dbContext);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new SubmitForReviewCommand(property.Id, ownerId, false), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ByNonOwnerNonAdmin_ThrowsForbiddenAccessException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddDraftProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new SubmitForReviewHandler(dbContext);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new SubmitForReviewCommand(property.Id, Guid.NewGuid(), false), CancellationToken.None));
        Assert.Equal(PropertyStatus.Draft, property.Status);
    }

    [Fact]
    public async Task Handle_ByAdminNonOwner_Succeeds()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var property = AddDraftProperty(dbContext, ownerId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new SubmitForReviewHandler(dbContext);
        var result = await handler.Handle(new SubmitForReviewCommand(property.Id, Guid.NewGuid(), true), CancellationToken.None);

        Assert.Equal("PendingReview", result!.Status);
    }

    [Fact]
    public async Task Handle_ForUnknownPropertyId_ReturnsNull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new SubmitForReviewHandler(dbContext);

        var result = await handler.Handle(new SubmitForReviewCommand(Guid.NewGuid(), Guid.NewGuid(), false), CancellationToken.None);

        Assert.Null(result);
    }
}
