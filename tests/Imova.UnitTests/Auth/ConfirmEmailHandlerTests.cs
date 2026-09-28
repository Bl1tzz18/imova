using FluentValidation;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Auth;
using Imova.Application.Features.Auth.ConfirmEmail;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Auth;

public class ConfirmEmailHandlerTests
{
    private readonly FakeUserStore _store = new();
    private readonly ImovaDbContext _dbContext = TestDbContextFactory.Create();

    private Task ConfirmAsync(Guid userId, string token) =>
        new ConfirmEmailHandler(TestUserManagerFactory.Create(_store), _dbContext)
            .Handle(new ConfirmEmailCommand(userId, token), CancellationToken.None);

    private static string LinkToken(ApplicationUser user) =>
        AccountTokens.Encode(FakeTokenProvider.TokenFor("EmailConfirmation", user));

    [Fact]
    public async Task Handle_WithTheEmailedToken_ConfirmsTheEmail()
    {
        var user = _store.SeedUser("ana@example.com", emailConfirmed: false);

        await ConfirmAsync(user.Id, LinkToken(user));

        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public async Task Handle_SubmitsTheOwnersWaitingDraftsForReview_AndNothingElse()
    {
        var user = _store.SeedUser("ana@example.com", emailConfirmed: false);
        var individual = ListingTestData.AddIndividualPublisher(_dbContext, user.Id);
        var agency = ListingTestData.AddAgencyPublisher(_dbContext, user.Id);
        var draft = ListingTestData.AddListing(_dbContext, individual.Id);
        var agencyDraft = ListingTestData.AddListing(_dbContext, agency.Id);
        var rejected = ListingTestData.AddListing(_dbContext, individual.Id).MoveTo(ListingStatus.Rejected);
        var someoneElsesDraft = ListingTestData.AddListing(_dbContext, ListingTestData.AddIndividualPublisher(_dbContext).Id);
        await _dbContext.SaveChangesAsync();

        await ConfirmAsync(user.Id, LinkToken(user));

        Assert.Equal(ListingStatus.PendingReview, draft.Status);
        Assert.Equal(ListingStatus.PendingReview, agencyDraft.Status);
        // A rejection needs the owner's fixes first — confirming doesn't resubmit it.
        Assert.Equal(ListingStatus.Rejected, rejected.Status);
        Assert.Equal(ListingStatus.Draft, someoneElsesDraft.Status);
    }

    [Fact]
    public async Task Handle_WithAWrongToken_FailsAsAnInvalidLink()
    {
        var user = _store.SeedUser("ana@example.com", emailConfirmed: false);

        var error = await Assert.ThrowsAsync<ValidationException>(() => ConfirmAsync(user.Id, AccountTokens.Encode("forged")));

        Assert.Contains(error.Errors, e => e.ErrorMessage == ConfirmEmailHandler.InvalidLink);
        Assert.False(user.EmailConfirmed);
    }

    [Fact]
    public async Task Handle_ForAnUnknownUser_FailsAsAnInvalidLink()
    {
        await Assert.ThrowsAsync<ValidationException>(() => ConfirmAsync(Guid.NewGuid(), AccountTokens.Encode("x")));
    }

    [Fact]
    public async Task Handle_WhenAlreadyConfirmed_Succeeds()
    {
        // Opening the link a second time shouldn't show an error.
        var user = _store.SeedUser("ana@example.com", emailConfirmed: true);

        await ConfirmAsync(user.Id, AccountTokens.Encode("already-used"));

        Assert.True(user.EmailConfirmed);
    }
}
