using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Properties.GetPendingReviewProperties;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class GetPendingReviewPropertiesHandlerTests
{
    private static Property AddProperty(ImovaDbContext dbContext, string title, PropertyStatus status)
    {
        var property = Property.Create(
            Guid.NewGuid(), title, "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);

        switch (status)
        {
            case PropertyStatus.PendingReview:
                property.SubmitForReview();
                break;
            case PropertyStatus.Published:
                property.SubmitForReview();
                property.Approve();
                break;
            case PropertyStatus.Draft:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }

        dbContext.Properties.Add(property);
        return property;
    }

    [Fact]
    public async Task Handle_ByAdmin_ReturnsOnlyPendingReviewListings()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var pending = AddProperty(dbContext, "Pending", PropertyStatus.PendingReview);
        AddProperty(dbContext, "Draft", PropertyStatus.Draft);
        AddProperty(dbContext, "Published", PropertyStatus.Published);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPendingReviewPropertiesHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPendingReviewPropertiesQuery(true), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(pending.Id, item.Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Handle_OrdersOldestSubmissionFirst()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var first = AddProperty(dbContext, "First submitted", PropertyStatus.PendingReview);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        var second = AddProperty(dbContext, "Second submitted", PropertyStatus.PendingReview);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPendingReviewPropertiesHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPendingReviewPropertiesQuery(true), CancellationToken.None);

        Assert.Equal([first.Id, second.Id], result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task Handle_RespectsPageAndPageSize()
    {
        await using var dbContext = TestDbContextFactory.Create();
        for (var i = 0; i < 5; i++)
        {
            AddProperty(dbContext, $"Listing {i}", PropertyStatus.PendingReview);
        }
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPendingReviewPropertiesHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPendingReviewPropertiesQuery(true, Page: 2, PageSize: 2), CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task Handle_ByNonAdmin_ThrowsForbiddenAccessException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetPendingReviewPropertiesHandler(dbContext, new FakeBlobStorageService());

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(new GetPendingReviewPropertiesQuery(false), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithNoPendingReviewListings_ReturnsEmptyResult()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetPendingReviewPropertiesHandler(dbContext, new FakeBlobStorageService());

        var result = await handler.Handle(new GetPendingReviewPropertiesQuery(true), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }
}
