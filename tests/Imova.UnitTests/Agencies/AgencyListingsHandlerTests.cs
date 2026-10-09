using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Agencies.Listings;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Agencies;

public class AgencyListingsHandlerTests : IAsyncDisposable
{
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
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
        var listing = ListingTestData.AddListing(_db, publisher.Id, agencyId: agencyId).MoveTo(status);
        await _db.SaveChangesAsync();
        return listing;
    }

    private Task<List<Imova.Contracts.Listings.ListingDto>?> ListAsync(Guid userId, bool isAdmin = false, Guid? agencyId = null) =>
        new GetAgencyListingsHandler(_db, new FakeBlobStorageService())
            .Handle(new GetAgencyListingsQuery(agencyId ?? _agency.Id, userId, isAdmin), CancellationToken.None);

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

        var listings = await ListAsync(manager);

        Assert.NotNull(listings);
        Assert.Equal(
            new[] { draft.Id, agentsRented.Id, active.Id }.Order(),
            listings.Select(l => l.Id).Order());
    }

    [Fact]
    public async Task AnAgent_SeesOnlyTheirOwnAgencyListings()
    {
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var own = await AddListingAsync(agent, _agency.Id, ListingStatus.PendingReview);
        await AddListingAsync(_owner, _agency.Id);

        var listings = await ListAsync(agent);

        Assert.Equal([own.Id], listings!.Select(l => l.Id));
    }

    [Fact]
    public async Task PrivateListings_AndOtherAgencies_AreLeftOut()
    {
        var other = ListingTestData.AddAgency(_db, _owner, "Alt Imobil");
        await _db.SaveChangesAsync();
        await AddListingAsync(_owner, agencyId: null);
        await AddListingAsync(_owner, other.Id);
        var mine = await AddListingAsync(_owner, _agency.Id);

        var listings = await ListAsync(_owner);

        Assert.Equal([mine.Id], listings!.Select(l => l.Id));
    }

    [Fact]
    public async Task NewestFirst()
    {
        var first = await AddListingAsync(_owner, _agency.Id);
        await Task.Delay(5);
        var second = await AddListingAsync(_owner, _agency.Id);

        var listings = await ListAsync(_owner);

        Assert.Equal([second.Id, first.Id], listings!.Select(l => l.Id));
    }

    [Fact]
    public async Task ANonMember_IsForbidden_ASiteAdminSeesEverything()
    {
        var listing = await AddListingAsync(_owner, _agency.Id, ListingStatus.Draft);
        var stranger = Guid.NewGuid();
        ListingTestData.AddUser(_db, stranger);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => ListAsync(stranger));
        Assert.Equal([listing.Id], (await ListAsync(stranger, isAdmin: true))!.Select(l => l.Id));
    }

    [Fact]
    public async Task AnUnknownAgency_IsNull()
    {
        Assert.Null(await ListAsync(_owner, agencyId: Guid.NewGuid()));
    }
}
