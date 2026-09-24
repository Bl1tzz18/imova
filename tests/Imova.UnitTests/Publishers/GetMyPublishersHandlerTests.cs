using Imova.Application.Common.Identity;
using Imova.Application.Features.Publishers.GetMyPublishers;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Publishers;

public class GetMyPublishersHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsIndividualThenAgency_WithContactDetails()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        ListingTestData.AddAgencyPublisher(dbContext, userId);
        ListingTestData.AddIndividualPublisher(dbContext, userId);
        ListingTestData.AddIndividualPublisher(dbContext);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await new GetMyPublishersHandler(dbContext).Handle(new GetMyPublishersQuery(userId), CancellationToken.None);

        Assert.Equal(["Individual", "Agency"], result.Select(p => p.PublisherType));
        Assert.All(result, p => Assert.NotNull(p.Email));
    }

    [Fact]
    public async Task Handle_ForUserWithoutAnyPublisher_ProvisionsTheirIndividualOne()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "vlad@example.com", UserName = "vlad@example.com", DisplayName = "Vlad" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await new GetMyPublishersHandler(dbContext).Handle(new GetMyPublishersQuery(user.Id), CancellationToken.None);

        var publisher = Assert.Single(result);
        Assert.Equal("Vlad", publisher.DisplayName);
        Assert.Single(dbContext.Publishers);
    }
}
