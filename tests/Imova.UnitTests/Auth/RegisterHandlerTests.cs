using Imova.Application.Common;
using Imova.Application.Features.Auth;
using Imova.Application.Features.Auth.Register;
using Imova.Domain.Publishers;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Auth;

public class RegisterHandlerTests
{
    private readonly FakeUserStore _store = new();
    private readonly FakeEmailSender _email = new();

    private RegisterHandler Handler(ImovaDbContext dbContext)
    {
        var userManager = TestUserManagerFactory.Create(_store);
        return new RegisterHandler(
            userManager,
            TestSessions.For(userManager, dbContext),
            dbContext,
            new AccountEmails(userManager, _email, new AppOptions { WebBaseUrl = "https://imova.test" }),
            NullLogger<RegisterHandler>.Instance);
    }

    [Fact]
    public async Task Handle_CreatesTheUserAndAnIndividualPublisherFromTheirAccountDetails()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await Handler(dbContext).Handle(
            new RegisterCommand("ana@example.com", "SuperSecret1!", "Ana Rusu", "+373 69 123 456"), CancellationToken.None);

        var user = Assert.Single(_store.Users);
        Assert.Equal(user.Id, result.User.Id);
        var publisher = Assert.Single(dbContext.Publishers);
        Assert.Equal(user.Id, publisher.UserId);
        Assert.Equal("Ana Rusu", publisher.DisplayName);
        Assert.Equal("+373 69 123 456", publisher.Phone);
        Assert.Equal("ana@example.com", publisher.Email);
    }

    [Fact]
    public async Task Handle_WithoutDisplayName_UsesTheEmailLocalPartAsThePublisherName()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await Handler(dbContext).Handle(
            new RegisterCommand("ana.rusu@example.com", "SuperSecret1!", null, "+373 69 123 456"), CancellationToken.None);

        Assert.Equal("ana.rusu", Assert.Single(dbContext.Publishers).DisplayName);
    }

    [Fact]
    public async Task Handle_LeavesTheEmailUnconfirmedAndSendsAConfirmationLink()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await Handler(dbContext).Handle(
            new RegisterCommand("ana@example.com", "SuperSecret1!", "Ana Rusu", "+373 69 123 456"), CancellationToken.None);

        Assert.False(result.User.EmailConfirmed);
        var email = Assert.Single(_email.Sent);
        Assert.Equal("ana@example.com", email.To);
        var token = AccountTokens.Encode(FakeTokenProvider.TokenFor("EmailConfirmation", Assert.Single(_store.Users)));
        Assert.Contains($"https://imova.test/confirm-email?userId={result.User.Id}&token={token}", email.TextBody);
        Assert.Contains("Ana Rusu", email.TextBody);
    }

    [Fact]
    public async Task Handle_WhenTheConfirmationEmailFails_StillRegisters()
    {
        await using var dbContext = TestDbContextFactory.Create();
        _email.Fail = true;

        var result = await Handler(dbContext).Handle(
            new RegisterCommand("ana@example.com", "SuperSecret1!", "Ana Rusu", "+373 69 123 456"), CancellationToken.None);

        Assert.Equal(Assert.Single(_store.Users).Id, result.User.Id);
        Assert.False(string.IsNullOrEmpty(result.Token));
    }
}
