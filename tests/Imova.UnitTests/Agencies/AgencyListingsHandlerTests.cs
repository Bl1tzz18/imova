using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Agencies.Listings;
using Imova.Application.Features.Listings;
using Imova.Contracts.Agencies;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Agencies;

public class AgencyListingsHandlerTests : IAsyncDisposable
{
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly ManualTimeProvider _clock = new(DateTimeOffset.UtcNow);
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Agency _agency;

    public AgencyListingsHandlerTests()
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
        _agency.AddMember(userId, role, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync();
        return userId;
    }

    private async Task<Listing> AddListingAsync(Guid userId, Guid? agencyId, ListingStatus status = ListingStatus.Active)
    {
        var publisher = await _db.Publishers.FirstOrDefaultAsync(p => p.UserId == userId)
            ?? ListingTestData.AddIndividualPublisher(_db, userId);
        var listing = ListingTestData.AddListing(_db, publisher.Id, agencyId: agencyId, photos: ListingPhotoRules.MinPhotos).MoveTo(status);
        await _db.SaveChangesAsync();
        return listing;
    }

    private Task<AgencyListingsPageDto?> ListAsync(
        Guid userId,
        bool isAdmin = false,
        Guid? agencyId = null,
        string? group = null,
        string? query = null,
        string? sort = null,
        int page = 1,
        int pageSize = GetAgencyListingsQuery.DefaultPageSize) =>
        new GetAgencyListingsHandler(_db, new FakeBlobStorageService(), _clock)
            .Handle(new GetAgencyListingsQuery(agencyId ?? _agency.Id, userId, isAdmin, group, query, sort, page, pageSize), CancellationToken.None);

    [Theory]
    [InlineData(AgencyRole.Owner)]
    [InlineData(AgencyRole.Admin)]
    public async Task OwnersAndAdmins_SeeEveryListingOfTheAgency_InEveryStatus(AgencyRole role)
    {
        var manager = role == AgencyRole.Owner ? _owner : await AddMemberAsync(AgencyRole.Admin);
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var draft = await AddListingAsync(_owner, _agency.Id, ListingStatus.Draft);
        var agentsRented = await AddListingAsync(agent, _agency.Id, ListingStatus.Rented);
        var active = await AddListingAsync(agent, _agency.Id);

        var result = (await ListAsync(manager))!;

        Assert.Equal("Active", result.Group);
        Assert.Equal([active.Id], result.Items.Select(l => l.Id));
        Assert.Equal(new AgencyListingGroupDto(1, false), result.Active);
        Assert.Equal(new AgencyListingGroupDto(1, true), result.Unpublished);
        Assert.Equal(new AgencyListingGroupDto(1, false), result.Ended);
        Assert.Equal([draft.Id], (await ListAsync(manager, group: "unpublished"))!.Items.Select(l => l.Id));
        Assert.Equal([agentsRented.Id], (await ListAsync(manager, group: "Ended"))!.Items.Select(l => l.Id));
    }

    [Fact]
    public async Task AnAgent_SeesOnlyTheirOwnAgencyListings()
    {
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var own = await AddListingAsync(agent, _agency.Id, ListingStatus.PendingReview);
        await AddListingAsync(_owner, _agency.Id);

        var result = (await ListAsync(agent))!;

        // Nothing Active of theirs: the first tab with anything opens.
        Assert.Equal("Unpublished", result.Group);
        Assert.Equal([own.Id], result.Items.Select(l => l.Id));
        Assert.Equal(0, result.Active.Count);
    }

    [Fact]
    public async Task PrivateListings_AndOtherAgencies_AreLeftOut()
    {
        var other = ListingTestData.AddAgency(_db, _owner, "Alt Imobil");
        await _db.SaveChangesAsync();
        await AddListingAsync(_owner, agencyId: null);
        await AddListingAsync(_owner, other.Id);
        var mine = await AddListingAsync(_owner, _agency.Id);

        var result = (await ListAsync(_owner))!;

        Assert.Equal([mine.Id], result.Items.Select(l => l.Id));
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Pages_DontOverlap_AndAPagePastTheEndShowsTheLast()
    {
        var ids = new List<Guid>();
        for (var i = 0; i < 30; i++)
        {
            ids.Add((await AddListingAsync(_owner, _agency.Id)).Id);
        }

        var first = (await ListAsync(_owner, sort: "newest", pageSize: 24))!;
        var second = (await ListAsync(_owner, sort: "newest", page: 2, pageSize: 24))!;
        var beyond = (await ListAsync(_owner, sort: "newest", page: 99, pageSize: 24))!;

        Assert.Equal(24, first.Items.Count);
        Assert.Equal(6, second.Items.Count);
        Assert.Equal(30, first.TotalCount);
        Assert.Equal(30, first.Active.Count);
        Assert.Equal(ids.Order(), first.Items.Concat(second.Items).Select(l => l.Id).Order());
        Assert.Equal(2, beyond.Page);
        Assert.Equal(second.Items.Select(l => l.Id), beyond.Items.Select(l => l.Id));
    }

    [Fact]
    public async Task ThePageSizeIsCapped()
    {
        Assert.Equal(GetAgencyListingsQuery.MaxPageSize, (await ListAsync(_owner, pageSize: 500))!.PageSize);
        Assert.Equal(1, (await ListAsync(_owner, pageSize: 0))!.PageSize);
    }

    [Fact]
    public async Task NewestFirst()
    {
        var first = await AddListingAsync(_owner, _agency.Id);
        await Task.Delay(5);
        var second = await AddListingAsync(_owner, _agency.Id);

        var result = (await ListAsync(_owner, sort: "newest"))!;

        Assert.Equal([second.Id, first.Id], result.Items.Select(l => l.Id));
    }

    [Fact]
    public async Task Recommended_PutsWhatNeedsTheOwnerFirst()
    {
        var draft = await AddListingAsync(_owner, _agency.Id, ListingStatus.Draft);
        await Task.Delay(5);
        var inReview = await AddListingAsync(_owner, _agency.Id, ListingStatus.PendingReview);

        var result = (await ListAsync(_owner, group: "unpublished"))!;

        Assert.Equal([draft.Id, inReview.Id], result.Items.Select(l => l.Id));
    }

    [Fact]
    public async Task AListingShortOfPhotos_NeedsAttention()
    {
        var publisher = ListingTestData.AddIndividualPublisher(_db, _owner);
        ListingTestData.AddListing(_db, publisher.Id, agencyId: _agency.Id, photos: 2).MoveTo(ListingStatus.Active);
        await _db.SaveChangesAsync();

        Assert.True((await ListAsync(_owner))!.Active.NeedsAttention);
    }

    [Fact]
    public async Task AnActiveListingNearItsEnd_NeedsAttention()
    {
        var listing = await AddListingAsync(_owner, _agency.Id);
        Assert.False((await ListAsync(_owner))!.Active.NeedsAttention);

        _clock.Now = listing.ExpiresAt!.Value.AddDays(-3);

        Assert.True((await ListAsync(_owner))!.Active.NeedsAttention);
    }

    [Fact]
    public async Task TheSearch_IgnoresCaseAndDiacritics_AndTheCountsFollowIt()
    {
        var match = await AddListingAsync(_owner, _agency.Id);
        await AddListingAsync(_owner, _agency.Id, ListingStatus.Draft);

        // The test listings are "Apartament 2 camere" on "Strada Ismail", Chișinău.
        var result = (await ListAsync(_owner, query: "APARTAMENT chisinau"))!;
        var none = (await ListAsync(_owner, query: "garsoniera"))!;

        Assert.Equal(1, result.Active.Count);
        Assert.Equal(1, result.Unpublished.Count);
        Assert.Contains(result.Items, l => l.Id == match.Id);
        Assert.Equal(0, none.Active.Count + none.Unpublished.Count + none.Ended.Count);
        Assert.Empty(none.Items);
    }

    [Fact]
    public async Task ANonMember_IsForbidden_ASiteAdminSeesEverything()
    {
        var listing = await AddListingAsync(_owner, _agency.Id, ListingStatus.Draft);
        var stranger = Guid.NewGuid();
        ListingTestData.AddUser(_db, stranger);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => ListAsync(stranger));
        Assert.Equal([listing.Id], (await ListAsync(stranger, isAdmin: true))!.Items.Select(l => l.Id));
    }

    [Fact]
    public async Task AnUnknownAgency_IsNull()
    {
        Assert.Null(await ListAsync(_owner, agencyId: Guid.NewGuid()));
    }

    [Theory]
    [InlineData("ended", AgencyListingGroup.Ended)]
    [InlineData("Active", AgencyListingGroup.Active)]
    [InlineData("7", null)]
    [InlineData("nonsense", null)]
    [InlineData(null, null)]
    public void GroupsParseByName_Only(string? value, AgencyListingGroup? expected)
    {
        Assert.Equal(expected, AgencyListingGroups.ParseGroup(value));
    }
}
