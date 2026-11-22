using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Agencies.Admin;
using Imova.Application.Features.Agencies.Lifecycle;
using Imova.Application.Features.Listings;
using Imova.Application.Features.Listings.GetListingById;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Agencies;

// Step 7: deactivate / reactivate / delete, and admin verification.
public class AgencyLifecycleTests : IAsyncDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly ManualTimeProvider _clock = new(Now);
    private readonly FakeBlobStorageService _blobs = new();
    private readonly FakeEmailSender _email = new();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Agency _agency;

    public AgencyLifecycleTests()
    {
        ListingTestData.AddUser(_db, _owner);
        _agency = ListingTestData.AddAgency(_db, _owner, "Casa Ta");
        _db.SaveChanges();
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private async Task<Guid> AddMemberAsync(AgencyRole role)
    {
        var userId = Guid.NewGuid();
        ListingTestData.AddUser(_db, userId);
        _agency.AddMember(userId, role, Now);
        await _db.SaveChangesAsync();
        return userId;
    }

    private async Task<Listing> AddAgencyListingAsync(Guid userId)
    {
        var publisher = await _db.Publishers.FirstOrDefaultAsync(p => p.UserId == userId)
            ?? ListingTestData.AddIndividualPublisher(_db, userId);
        var listing = ListingTestData.AddListing(_db, publisher.Id, agencyId: _agency.Id).MoveTo(ListingStatus.Active);
        await _db.SaveChangesAsync();
        return listing;
    }

    private Task<Imova.Contracts.Agencies.AgencyDto?> SetActiveAsync(Guid userId, bool active, bool isAdmin = false) =>
        new SetAgencyActiveHandler(_db, _blobs, _clock)
            .Handle(new SetAgencyActiveCommand(_agency.Id, userId, isAdmin, active), CancellationToken.None);

    private Task<bool> DeleteAsync(Guid userId, string? confirmName, bool isAdmin = false) =>
        new DeleteAgencyHandler(_db, _blobs, NullLogger<DeleteAgencyHandler>.Instance)
            .Handle(new DeleteAgencyCommand(_agency.Id, userId, isAdmin, confirmName), CancellationToken.None);

    private Task<bool> SetVerifiedAsync(bool verified, Guid? agencyId = null, Guid? adminId = null) =>
        new SetAgencyVerifiedHandler(
                _db,
                new AgencyVerifiedEmail(_email, new AppOptions { WebBaseUrl = "https://imova.md" }),
                _clock,
                NullLogger<SetAgencyVerifiedHandler>.Instance)
            .Handle(new SetAgencyVerifiedCommand(agencyId ?? _agency.Id, adminId ?? Guid.NewGuid(), verified), CancellationToken.None);

    // --- The domain ---

    [Fact]
    public void DeactivatingAndReactivating_AreIdempotent()
    {
        var agency = ListingTestData.AddAgency(_db, Guid.NewGuid());

        agency.Deactivate(Now);
        agency.Deactivate(Now.AddHours(1));
        Assert.Equal(AgencyStatus.Deactivated, agency.Status);
        Assert.Equal(Now, agency.UpdatedAt);

        agency.Reactivate(Now.AddHours(2));
        Assert.Equal(AgencyStatus.Active, agency.Status);
    }

    [Fact]
    public void VerifyingTwice_ChangesNothingTheSecondTime()
    {
        var agency = ListingTestData.AddAgency(_db, Guid.NewGuid());

        Assert.True(agency.Verify(Now));
        Assert.False(agency.Verify(Now.AddDays(1)));
        Assert.Equal(Now, agency.VerifiedAt);

        Assert.True(agency.Unverify(Now.AddDays(2)));
        Assert.False(agency.IsVerified);
        Assert.Null(agency.VerifiedAt);
        Assert.False(agency.Unverify(Now.AddDays(3)));
    }

    [Fact]
    public async Task VerifyingAndUnverifying_AreRecordedInTheAdminAudit_WhoWhenWhichAgency_OncePerChange()
    {
        var verifier = Guid.NewGuid();
        var unverifier = Guid.NewGuid();

        Assert.True(await SetVerifiedAsync(true, adminId: verifier));
        // Verifying again changes nothing, so nothing new is recorded.
        _clock.Now = Now.AddMinutes(5);
        Assert.True(await SetVerifiedAsync(true, adminId: unverifier));
        _clock.Now = Now.AddHours(2);
        Assert.True(await SetVerifiedAsync(false, adminId: unverifier));
        // An unknown agency: no change, no entry.
        Assert.False(await SetVerifiedAsync(true, agencyId: Guid.NewGuid(), adminId: verifier));

        var entries = await _db.AdminAuditEntries.OrderBy(e => e.CreatedAt).ToListAsync();
        Assert.Equal(
            [
                (AdminAuditEntry.VerifyAgency, verifier, (Guid?)_agency.Id, (Guid?)null, Now),
                (AdminAuditEntry.UnverifyAgency, unverifier, (Guid?)_agency.Id, (Guid?)null, Now.AddHours(2)),
            ],
            entries.Select(e => (e.Action, e.ActorUserId, e.TargetAgencyId, e.TargetUserId, e.CreatedAt)));
    }

    // --- Deactivate / reactivate ---

    [Theory]
    [InlineData(AgencyRole.Owner)]
    [InlineData(AgencyRole.Admin)]
    public async Task OwnersAndAdmins_Deactivate_AndReactivate(AgencyRole role)
    {
        var actor = role == AgencyRole.Owner ? _owner : await AddMemberAsync(AgencyRole.Admin);

        Assert.Equal("Deactivated", (await SetActiveAsync(actor, active: false))!.Status);
        Assert.Equal("Active", (await SetActiveAsync(actor, active: true))!.Status);
    }

    [Fact]
    public async Task AnAgent_IsForbidden_AndAnOutsiderCantSeeADeactivatedAgency()
    {
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var stranger = Guid.NewGuid();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => SetActiveAsync(agent, active: false));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => SetActiveAsync(stranger, active: false));

        await SetActiveAsync(_owner, active: false);
        Assert.Null(await SetActiveAsync(stranger, active: true));
        Assert.Equal("Active", (await SetActiveAsync(stranger, active: true, isAdmin: true))!.Status);
    }

    [Fact]
    public async Task ADeactivatedAgencysListings_AreHiddenFromThePublic_NotFromTheirAuthor()
    {
        var listing = await AddAgencyListingAsync(_owner);
        var privateListing = ListingTestData.AddListing(_db, ListingTestData.AddIndividualPublisher(_db).Id).MoveTo(ListingStatus.Active);
        await _db.SaveChangesAsync();
        var page = new GetListingByIdHandler(_db, _blobs);

        await SetActiveAsync(_owner, active: false);

        var visible = await _db.Listings.WherePublic(_db).Select(l => l.Id).ToListAsync();
        Assert.Equal([privateListing.Id], visible);
        Assert.Null(await page.Handle(new GetListingByIdQuery(listing.Id), CancellationToken.None));
        Assert.Null(await page.Handle(new GetListingByIdQuery(listing.Id, Guid.NewGuid()), CancellationToken.None));
        Assert.NotNull(await page.Handle(new GetListingByIdQuery(listing.Id, _owner), CancellationToken.None));
        Assert.NotNull(await page.Handle(new GetListingByIdQuery(listing.Id, Guid.NewGuid(), IsAdmin: true), CancellationToken.None));

        await SetActiveAsync(_owner, active: true);

        Assert.Contains(listing.Id, await _db.Listings.WherePublic(_db).Select(l => l.Id).ToListAsync());
        Assert.NotNull(await page.Handle(new GetListingByIdQuery(listing.Id), CancellationToken.None));
    }

    // --- Delete ---

    [Theory]
    [InlineData(null)]
    [InlineData("casa ta")]
    [InlineData("Casa")]
    public async Task Deleting_NeedsTheExactName(string? confirmName)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => DeleteAsync(_owner, confirmName));

        Assert.Contains(ex.Errors, e => e.ErrorCode == ErrorCodes.AgencyConfirmNameMismatch);
        Assert.True(await _db.Agencies.AnyAsync(a => a.Id == _agency.Id));
    }

    [Theory]
    [InlineData(AgencyRole.Admin)]
    [InlineData(AgencyRole.Agent)]
    public async Task OnlyAnOwner_Deletes(AgencyRole role)
    {
        var member = await AddMemberAsync(role);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => DeleteAsync(member, "Casa Ta"));
    }

    [Fact]
    public async Task Deleting_KeepsTheListingsAsPrivate_AndRemovesEverythingElse()
    {
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var ownersListing = await AddAgencyListingAsync(_owner);
        var agentsListing = await AddAgencyListingAsync(agent);
        _agency.SetLogo("agencies/x/logo-1_512.jpg", Now);
        _db.AgencyInvitations.Add(AgencyInvitation.Create(_agency.Id, "ana@example.com", AgencyRole.Agent, "hash", _owner, Now));
        _db.AgencyFormerSlugs.Add(AgencyFormerSlug.Create("casa-veche", _agency.Id, Now));
        await _db.SaveChangesAsync();

        Assert.True(await DeleteAsync(_owner, " Casa Ta "));

        Assert.False(await _db.Agencies.AnyAsync(a => a.Id == _agency.Id));
        Assert.False(await _db.AgencyMembers.AnyAsync(m => m.AgencyId == _agency.Id));
        Assert.False(await _db.AgencyInvitations.AnyAsync(i => i.AgencyId == _agency.Id));
        Assert.False(await _db.AgencyFormerSlugs.AnyAsync(s => s.AgencyId == _agency.Id));
        var listings = await _db.Listings.Where(l => l.Id == ownersListing.Id || l.Id == agentsListing.Id).ToListAsync();
        Assert.Equal(2, listings.Count);
        Assert.All(listings, l => Assert.Null(l.AgencyId));
        Assert.All(listings, l => Assert.Equal(ListingStatus.Active, l.Status));
        Assert.Equal(["agencies/x/logo-1_512.jpg", "agencies/x/logo-1_128.jpg"], _blobs.DeletedBlobNames);
    }

    [Fact]
    public async Task Deleting_RemovesItsImports_AndTheirKeptFiles()
    {
        var kept = ListingImportJob.Create(_agency.Id, _owner, "anunturi.csv", "imports/a/kept.csv", 100, Now);
        var gone = ListingImportJob.Create(_agency.Id, _owner, "vechi.csv", "imports/a/gone.csv", 100, Now.AddDays(-40));
        gone.Fail("import.fileUnreadable", Now.AddDays(-40));
        gone.MarkFileDeleted(Now.AddDays(-9));
        _db.ListingImportJobs.AddRange(kept, gone);
        await _db.SaveChangesAsync();

        Assert.True(await DeleteAsync(_owner, "Casa Ta"));

        Assert.Equal(["imports/a/kept.csv"], _blobs.DeletedPrivateNames);
    }

    [Fact]
    public async Task ASiteAdmin_Deletes_AnUnknownAgencyIsFalse()
    {
        Assert.True(await DeleteAsync(Guid.NewGuid(), "Casa Ta", isAdmin: true));
        Assert.False(await DeleteAsync(_owner, "Casa Ta"));
    }

    // --- A site admin deletes (from /admin/agencies) ---

    private Task<bool> AdminDeleteAsync(Guid adminId, string? confirmName, string? reason = "Agenție falsă, raportată de mai mulți utilizatori.", Guid? agencyId = null) =>
        new AdminDeleteAgencyHandler(
                _db,
                _blobs,
                new AgencyDeletedEmail(_email, new AppOptions { WebBaseUrl = "https://imova.md" }),
                _clock,
                NullLogger<AdminDeleteAgencyHandler>.Instance)
            .Handle(new AdminDeleteAgencyCommand(agencyId ?? _agency.Id, adminId, confirmName, reason, "203.0.113.7"), CancellationToken.None);

    [Fact]
    public async Task AnAdminsDelete_RemovesTheSameAsAnOwners_AndTouchesNothingOutsideTheAgency()
    {
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var ownersListing = await AddAgencyListingAsync(_owner);
        var agentsListing = await AddAgencyListingAsync(agent);
        _agency.SetLogo("agencies/x/logo-1_512.jpg", Now);
        _db.AgencyInvitations.Add(AgencyInvitation.Create(_agency.Id, "ana@example.com", AgencyRole.Agent, "hash", _owner, Now));
        _db.AgencyFormerSlugs.Add(AgencyFormerSlug.Create("casa-veche", _agency.Id, Now));
        var otherOwner = Guid.NewGuid();
        ListingTestData.AddUser(_db, otherOwner);
        var otherAgency = ListingTestData.AddAgency(_db, otherOwner, "Alta Agenție");
        var otherListing = ListingTestData.AddListing(_db, ListingTestData.AddIndividualPublisher(_db, otherOwner).Id, agencyId: otherAgency.Id)
            .MoveTo(ListingStatus.Active);
        await _db.SaveChangesAsync();

        Assert.True(await AdminDeleteAsync(Guid.NewGuid(), " Casa Ta "));

        Assert.False(await _db.Agencies.AnyAsync(a => a.Id == _agency.Id));
        Assert.False(await _db.AgencyMembers.AnyAsync(m => m.AgencyId == _agency.Id));
        Assert.False(await _db.AgencyInvitations.AnyAsync(i => i.AgencyId == _agency.Id));
        Assert.False(await _db.AgencyFormerSlugs.AnyAsync(s => s.AgencyId == _agency.Id));
        var listings = await _db.Listings.Where(l => l.Id == ownersListing.Id || l.Id == agentsListing.Id).ToListAsync();
        Assert.Equal(2, listings.Count);
        Assert.All(listings, l => Assert.Null(l.AgencyId));
        Assert.All(listings, l => Assert.Equal(ListingStatus.Active, l.Status));
        Assert.Equal(["agencies/x/logo-1_512.jpg", "agencies/x/logo-1_128.jpg"], _blobs.DeletedBlobNames);

        // The members' accounts and everything belonging to another agency stay.
        Assert.True(await _db.Users.AnyAsync(u => u.Id == _owner));
        Assert.True(await _db.Users.AnyAsync(u => u.Id == agent));
        Assert.True(await _db.Agencies.AnyAsync(a => a.Id == otherAgency.Id));
        Assert.Equal(otherAgency.Id, (await _db.Listings.SingleAsync(l => l.Id == otherListing.Id)).AgencyId);
    }

    [Fact]
    public async Task AnAdminsDelete_IsAudited_AndEmailsEveryMemberWithTheReason()
    {
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var admin = Guid.NewGuid();

        Assert.True(await AdminDeleteAsync(admin, "Casa Ta", "Date de contact false."));

        var entry = await _db.AdminAuditEntries.SingleAsync();
        Assert.Equal(
            (AdminAuditEntry.DeleteAgency, admin, (Guid?)_agency.Id, Now, "203.0.113.7"),
            (entry.Action, entry.ActorUserId, entry.TargetAgencyId, entry.CreatedAt, entry.IpAddress));
        Assert.Equal(new[] { $"{_owner:N}@example.com", $"{agent:N}@example.com" }.Order(), _email.Sent.Select(m => m.To).Order());
        Assert.All(_email.Sent, m => Assert.Contains("Casa Ta a fost ștearsă", m.Subject));
        Assert.All(_email.Sent, m => Assert.Contains("Date de contact false.", m.TextBody));
        Assert.All(_email.Sent, m => Assert.Contains("https://imova.md/my-listings", m.TextBody));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("casa ta")]
    [InlineData("Alta")]
    public async Task AnAdminsDelete_NeedsTheExactName_AndThenChangesNothing(string? confirmName)
    {
        var listing = await AddAgencyListingAsync(_owner);

        var error = await Assert.ThrowsAsync<ValidationException>(() => AdminDeleteAsync(Guid.NewGuid(), confirmName));

        Assert.Equal(ErrorCodes.AgencyConfirmNameMismatch, Assert.Single(error.Errors).ErrorCode);
        Assert.True(await _db.Agencies.AnyAsync(a => a.Id == _agency.Id));
        Assert.Equal(_agency.Id, (await _db.Listings.SingleAsync(l => l.Id == listing.Id)).AgencyId);
        Assert.Empty(await _db.AdminAuditEntries.ToListAsync());
        Assert.Empty(_email.Sent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void AnAdminsDelete_NeedsAReason(string? reason)
    {
        var result = new AdminDeleteAgencyValidator().Validate(new AdminDeleteAgencyCommand(_agency.Id, Guid.NewGuid(), "Casa Ta", reason, null));

        Assert.Equal(ErrorCodes.AgencyDeleteReasonRequired, Assert.Single(result.Errors).ErrorCode);
        Assert.False(new AdminDeleteAgencyValidator()
            .Validate(new AdminDeleteAgencyCommand(_agency.Id, Guid.NewGuid(), "Casa Ta", new string('x', 1001), null)).IsValid);
    }

    [Fact]
    public async Task AnAdminsDelete_OfAnUnknownAgency_IsFalse()
    {
        Assert.False(await AdminDeleteAsync(Guid.NewGuid(), "Casa Ta", agencyId: Guid.NewGuid()));
        Assert.True(await _db.Agencies.AnyAsync(a => a.Id == _agency.Id));
    }

    [Fact]
    public void TheDeletionEmail_IsInTheMembersLanguage_AndEncodesTheReason()
    {
        var message = AgencyDeletedEmail.Build("ana@example.com", "Casa Ta", "Spam <b>repetat</b>", "https://imova.md/my-listings", "ru", privacyUrl: "https://imova.test/privacy");

        Assert.Contains("Агентство Casa Ta удалено", message.Subject);
        Assert.Contains("Spam &lt;b&gt;repetat&lt;/b&gt;", message.HtmlBody);
        Assert.Contains("Spam <b>repetat</b>", message.TextBody);
    }

    // --- Verification ---

    [Fact]
    public async Task Verifying_EmailsTheOwners_Once()
    {
        var secondOwner = await AddMemberAsync(AgencyRole.Owner);
        await AddMemberAsync(AgencyRole.Agent);

        Assert.True(await SetVerifiedAsync(true));
        Assert.True(await SetVerifiedAsync(true));

        Assert.True((await _db.Agencies.SingleAsync(a => a.Id == _agency.Id)).IsVerified);
        Assert.Equal(
            new[] { $"{_owner:N}@example.com", $"{secondOwner:N}@example.com" }.Order(),
            _email.Sent.Select(m => m.To).Order());
        Assert.Contains("Casa Ta a fost verificată", _email.Sent[0].Subject);
        Assert.Contains("https://imova.md/agencies/", _email.Sent[0].TextBody);
    }

    [Fact]
    public async Task Unverifying_SendsNothing_AnUnknownAgencyIsFalse()
    {
        await SetVerifiedAsync(true);
        _email.Sent.Clear();

        Assert.True(await SetVerifiedAsync(false));

        Assert.False((await _db.Agencies.SingleAsync(a => a.Id == _agency.Id)).IsVerified);
        Assert.Empty(_email.Sent);
        Assert.False(await SetVerifiedAsync(true, Guid.NewGuid()));
    }

    [Fact]
    public async Task TheAdminList_PutsUnverifiedFirst_AndFilters()
    {
        _agency.Verify(Now);
        var newer = ListingTestData.AddAgency(_db, _owner, "Imobil Nou");
        var older = ListingTestData.AddAgency(_db, _owner, "Imobil Vechi");
        await _db.SaveChangesAsync();
        var handler = new GetAdminAgenciesHandler(_db, _blobs);

        var all = await handler.Handle(new GetAdminAgenciesQuery(null, null), CancellationToken.None);
        var unverified = await handler.Handle(new GetAdminAgenciesQuery(null, Verified: false), CancellationToken.None);
        var byName = await handler.Handle(new GetAdminAgenciesQuery("vechi", null), CancellationToken.None);

        Assert.Equal(3, all.TotalCount);
        Assert.Equal(_agency.Id, all.Items[^1].Id);
        Assert.All(all.Items.Take(2), a => Assert.False(a.IsVerified));
        Assert.Equal(new[] { newer.Id, older.Id }.Order(), unverified.Items.Select(a => a.Id).Order());
        Assert.Equal([older.Id], byName.Items.Select(a => a.Id));
        Assert.Equal($"{_owner:N}@example.com", all.Items[0].OwnerEmail);
        Assert.Equal(1, all.Items[0].MemberCount);
    }
}
