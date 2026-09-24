using Imova.Application.Features.Auth.Register;
using Imova.Domain.Publishers;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Auth;

public class RegisterHandlerTests
{
    [Fact]
    public async Task Handle_CreatesTheUserAndAnIndividualPublisherFromTheirAccountDetails()
    {
        var store = new FakeUserStore();
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new RegisterHandler(TestUserManagerFactory.Create(store), new FakeJwtTokenGenerator(), dbContext);

        var result = await handler.Handle(
            new RegisterCommand("ana@example.com", "SuperSecret1", "Ana Rusu", "+373 69 123 456"), CancellationToken.None);

        var user = Assert.Single(store.Users);
        Assert.Equal(user.Id, result.User.Id);
        var publisher = Assert.Single(dbContext.Publishers);
        Assert.Equal(user.Id, publisher.UserId);
        Assert.Equal(PublisherType.Individual, publisher.PublisherType);
        Assert.Equal("Ana Rusu", publisher.DisplayName);
        Assert.Equal("+373 69 123 456", publisher.Phone);
        Assert.Equal("ana@example.com", publisher.Email);
    }

    [Fact]
    public async Task Handle_WithoutDisplayName_UsesTheEmailLocalPartAsThePublisherName()
    {
        var store = new FakeUserStore();
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new RegisterHandler(TestUserManagerFactory.Create(store), new FakeJwtTokenGenerator(), dbContext);

        await handler.Handle(new RegisterCommand("ana.rusu@example.com", "SuperSecret1", null, "+373 69 123 456"), CancellationToken.None);

        Assert.Equal("ana.rusu", Assert.Single(dbContext.Publishers).DisplayName);
    }
}
