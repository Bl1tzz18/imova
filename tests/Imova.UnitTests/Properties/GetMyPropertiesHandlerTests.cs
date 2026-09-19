using Imova.Application.Features.Properties.GetMyProperties;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class GetMyPropertiesHandlerTests
{
    private static Property AddProperty(ImovaDbContext dbContext, Guid ownerId, string title)
    {
        var property = Property.Create(
            ownerId, title, "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        dbContext.Properties.Add(property);
        return property;
    }

    [Fact]
    public async Task Handle_ReturnsOnlyThatOwnersProperties()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var mine = AddProperty(dbContext, ownerId, "Mine");
        AddProperty(dbContext, Guid.NewGuid(), "Someone else's");
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMyPropertiesHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetMyPropertiesQuery(ownerId), CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(mine.Id, dto.Id);
    }

    [Fact]
    public async Task Handle_IncludesEveryStatusNotJustPublished()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var draft = AddProperty(dbContext, ownerId, "Draft");
        var published = AddProperty(dbContext, ownerId, "Published");
        published.SubmitForReview();
        published.Approve();
        var archived = AddProperty(dbContext, ownerId, "Archived");
        archived.SubmitForReview();
        archived.Approve();
        archived.Archive();
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMyPropertiesHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetMyPropertiesQuery(ownerId), CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, p => p.Id == draft.Id && p.Status == "Draft");
        Assert.Contains(result, p => p.Id == published.Id && p.Status == "Published");
        Assert.Contains(result, p => p.Id == archived.Id && p.Status == "Archived");
    }

    [Fact]
    public async Task Handle_OrdersMostRecentlyCreatedFirst()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var first = AddProperty(dbContext, ownerId, "First");
        await dbContext.SaveChangesAsync(CancellationToken.None);
        var second = AddProperty(dbContext, ownerId, "Second");
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMyPropertiesHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetMyPropertiesQuery(ownerId), CancellationToken.None);

        Assert.Equal([second.Id, first.Id], result.Select(p => p.Id));
    }

    [Fact]
    public async Task Handle_WithNoOwnedProperties_ReturnsEmptyList()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetMyPropertiesHandler(dbContext, new FakeBlobStorageService());

        var result = await handler.Handle(new GetMyPropertiesQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(result);
    }
}
