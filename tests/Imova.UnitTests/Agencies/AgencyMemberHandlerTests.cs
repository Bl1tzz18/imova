using System.Text.RegularExpressions;
using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Account;
using Imova.Application.Features.Agencies.Invitations;
using Imova.Application.Features.Agencies.Members;
using Imova.Domain.Agencies;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Agencies;

public class AgencyMemberHandlerTests : IAsyncDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly ManualTimeProvider _clock = new(Start);
    private readonly FakeEmailSender _email = new();
    private readonly FakeBlobStorageService _blobs = new();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Agency _agency;

    public AgencyMemberHandlerTests()
    {
        ListingTestData.AddUser(_db, _owner);
        _agency = Agency.Create(new AgencyProfile("Casa Ta", "+373 22 555 010", "o@casata.md"), "casa-ta", _owner, Start.AddDays(-30));
        _db.Agencies.Add(_agency);
        _db.SaveChanges();
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private async Task<Guid> AddMemberAsync(AgencyRole role, DateTimeOffset? joinedAt = null)
    {
        var userId = Guid.NewGuid();
        ListingTestData.AddUser(_db, userId);
        _agency.AddMember(userId, role, joinedAt ?? Start);
        await _db.SaveChangesAsync();
        return userId;
    }

    // A listing `userId` published under the agency.
    private async Task<Imova.Domain.Listings.Listing> AddAgencyListingAsync(Guid userId)
    {
        var publisher = await _db.Publishers.FirstOrDefaultAsync(p => p.UserId == userId)
            ?? ListingTestData.AddIndividualPublisher(_db, userId);
        var listing = ListingTestData.AddListing(_db, publisher.Id, agencyId: _agency.Id);
        await _db.SaveChangesAsync();
        return listing;
    }

    private async Task<Guid> AuthorOfAsync(Guid listingId)
    {
        var publisherId = (await _db.Listings.SingleAsync(l => l.Id == listingId)).PublisherId;
        return (await _db.Publishers.SingleAsync(p => p.Id == publisherId)).UserId;
    }

    private Task<bool> RemoveAsync(Guid actor, Guid member, Guid? reassignTo = null) =>
        new RemoveMemberHandler(_db, _clock).Handle(new RemoveMemberCommand(_agency.Id, actor, false, member, reassignTo), CancellationToken.None);

    private AgencyInvitationEmail InvitationEmail() => new(_email, new AppOptions { WebBaseUrl = "https://imova.md" });

    private Task<Imova.Contracts.Agencies.AgencyInvitationDto?> InviteAsync(string email, AgencyRole role = AgencyRole.Agent, Guid? actor = null) =>
        new InviteMemberHandler(_db, InvitationEmail(), _clock, NullLogger<InviteMemberHandler>.Instance)
            .Handle(new InviteMemberCommand(_agency.Id, actor ?? _owner, false, email, role), CancellationToken.None);

    private string LastToken() =>
        Regex.Match(_email.Sent[^1].TextBody, @"/invitations/([A-Za-z0-9_-]+)").Groups[1].Value;

    private Task<Imova.Contracts.Agencies.InvitationDto?> AcceptAsync(string? token, Guid userId, Guid? invitationId = null) =>
        new AcceptInvitationHandler(_db, _blobs, _clock).Handle(new AcceptInvitationCommand(token, invitationId, userId), CancellationToken.None);

    private async Task<Guid> AddUserWithEmailAsync(string email, bool confirmed = true)
    {
        var userId = Guid.NewGuid();
        var user = ListingTestData.AddUser(_db, userId, confirmed);
        user.Email = email;
        await _db.SaveChangesAsync();
        return userId;
    }

    // --- Removing and leaving ---

    [Fact]
    public async Task Removing_AnAgent_HandsTheirListingsToWhoeverRemovedThem()
    {
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var listing = await AddAgencyListingAsync(agent);

        Assert.True(await RemoveAsync(_owner, agent));

        Assert.False(_agency.IsMember(agent));
        Assert.Equal(_owner, await AuthorOfAsync(listing.Id));
        Assert.Equal(_agency.Id, (await _db.Listings.SingleAsync()).AgencyId);
    }

    [Fact]
    public async Task Removing_WithAChosenHeir_HandsTheListingsToThem()
    {
        var admin = await AddMemberAsync(AgencyRole.Admin);
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var listing = await AddAgencyListingAsync(agent);

        await RemoveAsync(_owner, agent, reassignTo: admin);

        Assert.Equal(admin, await AuthorOfAsync(listing.Id));
    }

    [Fact]
    public async Task Removing_WithAnHeirWhoIsOnlyAnAgent_IsRefused_AndNothingChanges()
    {
        var otherAgent = await AddMemberAsync(AgencyRole.Agent);
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var listing = await AddAgencyListingAsync(agent);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => RemoveAsync(_owner, agent, reassignTo: otherAgent));

        Assert.Equal(ErrorCodes.AgencyReassignInvalid, Assert.Single(ex.Errors).ErrorCode);
        Assert.Equal(agent, await AuthorOfAsync(listing.Id));
        Assert.True((await _db.Agencies.Include(a => a.Members).SingleAsync()).IsMember(agent));
    }

    [Fact]
    public async Task Leaving_HandsTheListingsToTheLongestStandingOwner()
    {
        var secondOwner = await AddMemberAsync(AgencyRole.Owner, joinedAt: Start);
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var listing = await AddAgencyListingAsync(agent);

        Assert.True(await RemoveAsync(agent, agent));

        // _owner joined 30 days before secondOwner.
        Assert.Equal(_owner, await AuthorOfAsync(listing.Id));
        Assert.True(_agency.IsMember(secondOwner));
    }

    [Fact]
    public async Task TheLastOwner_CantLeave()
    {
        await AddMemberAsync(AgencyRole.Agent);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => RemoveAsync(_owner, _owner));

        Assert.Equal(ErrorCodes.AgencyLastOwner, Assert.Single(ex.Errors).ErrorCode);
    }

    [Fact]
    public async Task AnAdmin_CantRemoveAnotherAdmin()
    {
        var admin = await AddMemberAsync(AgencyRole.Admin);
        var otherAdmin = await AddMemberAsync(AgencyRole.Admin);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => RemoveAsync(admin, otherAdmin));
    }

    // --- Roles and the member list ---

    [Fact]
    public async Task ChangingARole_ByTheOwner_Works_ButNotDemotingTheLastOwner()
    {
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var handler = new ChangeMemberRoleHandler(_db, _clock);

        Assert.True(await handler.Handle(new ChangeMemberRoleCommand(_agency.Id, _owner, false, agent, AgencyRole.Admin), CancellationToken.None));
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new ChangeMemberRoleCommand(_agency.Id, _owner, false, _owner, AgencyRole.Agent), CancellationToken.None));

        Assert.Equal(AgencyRole.Admin, _agency.RoleOf(agent));
        Assert.Equal(ErrorCodes.AgencyLastOwner, Assert.Single(ex.Errors).ErrorCode);
    }

    [Fact]
    public async Task TheMemberList_IsForMembers_WithEachOnesListingCount()
    {
        var agent = await AddMemberAsync(AgencyRole.Agent);
        await AddAgencyListingAsync(agent);
        await AddAgencyListingAsync(agent);
        var handler = new GetAgencyMembersHandler(_db);

        var members = (await handler.Handle(new GetAgencyMembersQuery(_agency.Id, agent, false), CancellationToken.None))!;

        Assert.Equal(["Owner", "Agent"], members.Select(m => m.Role));
        Assert.Equal(2, members.Single(m => m.UserId == agent).ListingCount);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(new GetAgencyMembersQuery(_agency.Id, Guid.NewGuid(), false), CancellationToken.None));
    }

    // --- Inviting ---

    [Fact]
    public async Task Inviting_EmailsALinkThatShowsTheInvitation()
    {
        var dto = await InviteAsync("Ana@Example.com");

        var sent = Assert.Single(_email.Sent);
        Assert.Equal("ana@example.com", sent.To);
        Assert.Contains("Casa Ta", sent.Subject);
        Assert.Equal("Pending", dto!.Status);
        var shown = await new GetInvitationByTokenHandler(_db, _blobs, _clock).Handle(new GetInvitationByTokenQuery(LastToken()), CancellationToken.None);
        Assert.Equal("Casa Ta", shown!.AgencyName);
        Assert.Equal("Agent", shown.Role);
        Assert.Null(await new GetInvitationByTokenHandler(_db, _blobs, _clock).Handle(new GetInvitationByTokenQuery("nope"), CancellationToken.None));
    }

    [Fact]
    public async Task AnInvitationWhoseEmailFailed_SaysSo_UntilAResendGoesThrough()
    {
        _email.Fail = true;
        var dto = await InviteAsync("ana@example.com");

        Assert.Equal(Start, dto!.EmailFailedAt);
        Assert.Equal(Start, (await _db.AgencyInvitations.SingleAsync()).EmailFailedAt);

        _email.Fail = false;
        _clock.Advance(TimeSpan.FromMinutes(11));
        var resent = await new ResendAgencyInvitationHandler(_db, InvitationEmail(), _clock, NullLogger<ResendAgencyInvitationHandler>.Instance)
            .Handle(new ResendAgencyInvitationCommand(_agency.Id, _owner, false, dto.Id), CancellationToken.None);

        Assert.Null(resent!.EmailFailedAt);
        Assert.Null((await _db.AgencyInvitations.SingleAsync()).EmailFailedAt);
    }

    [Fact]
    public async Task TheInvitedPerson_IsToldWhenTheAgencyIsInactive()
    {
        await InviteAsync("ana@example.com");
        var token = LastToken();
        var handler = new GetInvitationByTokenHandler(_db, _blobs, _clock);
        Assert.True((await handler.Handle(new GetInvitationByTokenQuery(token), CancellationToken.None))!.AgencyIsActive);

        // No deactivate action exists yet (step 7); set the status the way the database would hold it.
        _db.Entry(_agency).Property(a => a.Status).CurrentValue = AgencyStatus.Deactivated;
        await _db.SaveChangesAsync();

        Assert.False((await handler.Handle(new GetInvitationByTokenQuery(token), CancellationToken.None))!.AgencyIsActive);
    }

    [Fact]
    public async Task InvitingSomeoneAlreadyAMember_Is409()
    {
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var email = (await _db.Users.SingleAsync(u => u.Id == agent)).Email!;

        var ex = await Assert.ThrowsAsync<ConflictException>(() => InviteAsync(email.ToUpperInvariant()));

        Assert.Equal(ErrorCodes.AgencyAlreadyMember, ex.Code);
    }

    [Fact]
    public async Task InvitingTheSameAddressAgain_ResendsTheSameInvitation_AfterACooldown()
    {
        await InviteAsync("ana@example.com");
        var firstToken = LastToken();

        var tooSoon = await Assert.ThrowsAsync<TooManyRequestsException>(() => InviteAsync("ana@example.com"));
        Assert.Equal(ErrorCodes.AgencyInvitationResendTooSoon, tooSoon.Code);

        _clock.Advance(TimeSpan.FromMinutes(11));
        await InviteAsync("ana@example.com", AgencyRole.Admin);

        var invitation = Assert.Single(_db.AgencyInvitations);
        Assert.Equal(AgencyRole.Admin, invitation.Role);
        Assert.NotEqual(firstToken, LastToken());
        Assert.Null(await new GetInvitationByTokenHandler(_db, _blobs, _clock).Handle(new GetInvitationByTokenQuery(firstToken), CancellationToken.None));
    }

    [Fact]
    public async Task AnAgency_SendsAtMost20InvitationsAnHour()
    {
        for (var i = 0; i < 20; i++)
        {
            await InviteAsync($"agent{i}@example.com");
        }

        var ex = await Assert.ThrowsAsync<TooManyRequestsException>(() => InviteAsync("one-more@example.com"));

        Assert.Equal(ErrorCodes.AgencyInvitationRateLimit, ex.Code);
        _clock.Advance(TimeSpan.FromHours(1));
        Assert.NotNull(await InviteAsync("one-more@example.com"));
    }

    [Fact]
    public async Task AnAgency_HasAtMost50InvitationsWaiting()
    {
        for (var i = 0; i < 50; i++)
        {
            _db.AgencyInvitations.Add(AgencyInvitation.Create(_agency.Id, $"p{i}@example.com", AgencyRole.Agent, $"hash-{i}", _owner, Start.AddHours(-2)));
        }

        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => InviteAsync("fifty-one@example.com"));

        Assert.Equal(ErrorCodes.AgencyTooManyInvitations, Assert.Single(ex.Errors).ErrorCode);
    }

    [Fact]
    public async Task AnAgent_CantInvite_AndAnAdminCantInviteAnAdmin()
    {
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var admin = await AddMemberAsync(AgencyRole.Admin);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InviteAsync("x@example.com", actor: agent));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InviteAsync("x@example.com", AgencyRole.Admin, admin));
        Assert.NotNull(await InviteAsync("x@example.com", AgencyRole.Agent, admin));
    }

    // --- Accepting and declining ---

    [Fact]
    public async Task Accepting_WithTheInvitedAccount_MakesThemAMember()
    {
        await InviteAsync("ana@example.com", AgencyRole.Admin);
        var ana = await AddUserWithEmailAsync("Ana@Example.com");

        var dto = await AcceptAsync(LastToken(), ana);

        Assert.Equal("Accepted", dto!.Status);
        Assert.Equal(AgencyRole.Admin, (await _db.Agencies.Include(a => a.Members).SingleAsync()).RoleOf(ana));
    }

    [Fact]
    public async Task Accepting_WithAnotherAccount_IsForbidden()
    {
        await InviteAsync("ana@example.com");
        var someoneElse = await AddUserWithEmailAsync("ion@example.com");

        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() => AcceptAsync(LastToken(), someoneElse));

        Assert.Equal(ErrorCodes.AgencyInvitationWrongAccount, ex.Code);
        Assert.False(_agency.IsMember(someoneElse));
    }

    [Fact]
    public async Task Accepting_AnExpiredOrRevokedInvitation_IsRefused()
    {
        var ana = await AddUserWithEmailAsync("ana@example.com");
        await InviteAsync("ana@example.com");
        var expiredToken = LastToken();
        _clock.Advance(TimeSpan.FromDays(8));

        var expired = await Assert.ThrowsAsync<ValidationException>(() => AcceptAsync(expiredToken, ana));
        Assert.Equal(ErrorCodes.AgencyInvitationExpired, Assert.Single(expired.Errors).ErrorCode);

        var invitation = await InviteAsync("ana@example.com");
        await new RevokeAgencyInvitationHandler(_db, _clock)
            .Handle(new RevokeAgencyInvitationCommand(_agency.Id, _owner, false, invitation!.Id), CancellationToken.None);
        var revoked = await Assert.ThrowsAsync<ValidationException>(() => AcceptAsync(LastToken(), ana));
        Assert.Equal(ErrorCodes.AgencyInvitationClosed, Assert.Single(revoked.Errors).ErrorCode);
    }

    [Fact]
    public async Task Declining_ByTheLink_EndsIt()
    {
        await InviteAsync("ana@example.com");

        Assert.True(await new DeclineInvitationHandler(_db, _clock).Handle(new DeclineInvitationCommand(LastToken(), null, null), CancellationToken.None));

        Assert.Equal(AgencyInvitationStatus.Declined, (await _db.AgencyInvitations.SingleAsync()).StatusAt(_clock.Now));
    }

    [Fact]
    public async Task MyInvitations_AreOnlyForAConfirmedEmail_AndCanBeAcceptedById()
    {
        await InviteAsync("ana@example.com");
        var unconfirmed = await AddUserWithEmailAsync("ana@example.com", confirmed: false);
        var handler = new GetMyInvitationsHandler(_db, _blobs, _clock);

        Assert.Empty(await handler.Handle(new GetMyInvitationsQuery(unconfirmed), CancellationToken.None));
        Assert.Null(await AcceptAsync(null, unconfirmed, (await _db.AgencyInvitations.SingleAsync()).Id));

        var confirmed = await AddUserWithEmailAsync("ana@example.com");
        var mine = Assert.Single(await handler.Handle(new GetMyInvitationsQuery(confirmed), CancellationToken.None));
        Assert.Equal("Accepted", (await AcceptAsync(null, confirmed, mine.Id))!.Status);
    }

    // --- Deleting an account ---

    private AccountDeletion Deletion() =>
        new(_db, _blobs, new AccountDeletionEmails(TestUserManagerFactory.Create(new FakeUserStore()), _email, new AppOptions()),
            NullLogger<AccountDeletion>.Instance);

    [Fact]
    public async Task DeletingTheAccountOfTheLastOwner_OfAnAgencyWithOthers_IsRefused_AndNothingIsDeleted()
    {
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var listing = await AddAgencyListingAsync(_owner);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => Deletion().DeleteAsync(_owner, CancellationToken.None));

        Assert.Equal(ErrorCodes.AccountLastAgencyOwner, Assert.Single(ex.Errors).ErrorCode);
        Assert.True(await _db.Users.AnyAsync(u => u.Id == _owner));
        Assert.True(await _db.Listings.AnyAsync(l => l.Id == listing.Id));
        Assert.True(_agency.IsMember(agent));
    }

    [Fact]
    public async Task DeletingAMembersAccount_HandsTheirAgencyListingsOn_AndDeletesOnlyTheirOwn()
    {
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var agencyListing = await AddAgencyListingAsync(agent);
        var personal = ListingTestData.AddListing(_db, (await _db.Publishers.SingleAsync(p => p.UserId == agent)).Id);
        await _db.SaveChangesAsync();

        Assert.True(await Deletion().DeleteAsync(agent, CancellationToken.None));

        Assert.Equal(_owner, await AuthorOfAsync(agencyListing.Id));
        Assert.False(await _db.Listings.AnyAsync(l => l.Id == personal.Id));
        Assert.False(await _db.AgencyMembers.AnyAsync(m => m.UserId == agent));
        Assert.True(await _db.Agencies.AnyAsync());
    }

    [Fact]
    public async Task ADeletionLink_IsRefusedToTheLastOwner_OfAnAgencyWithOthers()
    {
        await AddMemberAsync(AgencyRole.Agent);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => Deletion().EnsureCanDeleteAsync(_owner, CancellationToken.None));

        Assert.Equal(ErrorCodes.AccountLastAgencyOwner, Assert.Single(ex.Errors).ErrorCode);
    }
}
