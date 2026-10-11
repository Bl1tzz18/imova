using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Agencies.Stats;
using Imova.Application.Features.Listings;
using Imova.Contracts.Agencies;
using Imova.Domain.Agencies;
using Imova.Domain.Favorites;
using Imova.Domain.Listings;
using Imova.Domain.Messaging;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Agencies;

public class AgencyStatsHandlerTests : IAsyncDisposable
{
    // Noon in Chișinău (UTC+3 in October) — 9 October 2026 there.
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 9);

    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly ManualTimeProvider _clock = new(Now);
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Agency _agency;

    public AgencyStatsHandlerTests()
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

    private async Task<Listing> AddListingAsync(Guid author, int photos = ListingPhotoRules.MinPhotos, Guid? agencyId = null)
    {
        var publisher = await _db.Publishers.FirstOrDefaultAsync(p => p.UserId == author) ?? ListingTestData.AddIndividualPublisher(_db, author);
        var listing = ListingTestData.AddListing(_db, publisher.Id, agencyId: agencyId ?? _agency.Id, photos: photos).MoveTo(ListingStatus.Active);
        await _db.SaveChangesAsync();
        return listing;
    }

    private void Day(Listing listing, DateOnly day, int views, int reveals) =>
        _db.ListingDailyStats.Add(ListingDailyStat.Create(listing.Id, day, views, reveals));

    private Task<AgencyStatsDto?> StatsAsync(Guid userId, DateOnly? from = null, DateOnly? to = null, bool isAdmin = false) =>
        new GetAgencyStatsHandler(_db, new FakeBlobStorageService(), _clock)
            .Handle(new GetAgencyStatsQuery(_agency.Id, userId, isAdmin, from, to), CancellationToken.None);

    [Fact]
    public async Task Totals_LeadWithContacts_AndCompareWithThePeriodBefore()
    {
        var listing = await AddListingAsync(_owner);
        Day(listing, Today, views: 40, reveals: 3);
        Day(listing, Today.AddDays(-6), views: 10, reveals: 1);
        Day(listing, Today.AddDays(-8), views: 5, reveals: 2); // the period before
        _db.Conversations.Add(Conversation.Start(listing.Id, Guid.NewGuid(), _owner, Now.AddHours(-1)));
        _db.Favorites.Add(Favorite.Create(Guid.NewGuid(), listing.Id, now: Now.AddDays(-2)));
        _db.AgencyPhoneReveals.Add(AgencyPhoneReveal.Create(_agency.Id, Today, "v1", Now));
        await _db.SaveChangesAsync();

        var stats = (await StatsAsync(_owner, from: Today.AddDays(-6), to: Today))!;

        Assert.Equal(new AgencyStatsTotalsDto(Contacts: 6, Views: 50, ListingPhoneReveals: 4, AgencyPhoneReveals: 1, Messages: 1, Favorites: 1), stats.Totals);
        Assert.Equal(2, stats.Previous.ListingPhoneReveals);
        Assert.Equal(7, stats.Daily.Count);
        Assert.Equal(new AgencyStatsDayDto(Today, 40, 5), stats.Daily[^1]);
        Assert.Equal(new AgencyStatsDayDto(Today.AddDays(-5), 0, 0), stats.Daily[1]);
    }

    [Fact]
    public async Task ByDefault_TheLast30DaysEndingToday()
    {
        var stats = (await StatsAsync(_owner))!;

        Assert.Equal(Today, stats.To);
        Assert.Equal(Today.AddDays(-29), stats.From);
        Assert.Equal(30, stats.Daily.Count);
    }

    [Fact]
    public async Task TopListings_ByContactsThenViews_WithoutOtherAgencies()
    {
        var quiet = await AddListingAsync(_owner);
        var popular = await AddListingAsync(_owner);
        var otherAgency = ListingTestData.AddAgency(_db, _owner, "Alt");
        await _db.SaveChangesAsync();
        var elsewhere = await AddListingAsync(_owner, agencyId: otherAgency.Id);
        Day(quiet, Today, views: 90, reveals: 0);
        Day(popular, Today, views: 10, reveals: 4);
        Day(elsewhere, Today, views: 500, reveals: 50);
        await _db.SaveChangesAsync();

        var stats = (await StatsAsync(_owner))!;

        Assert.Equal([popular.Id, quiet.Id], stats.TopListings.Select(l => l.ListingId));
        Assert.Equal(4, stats.TopListings[0].PhoneReveals);
    }

    [Fact]
    public async Task NeedsAttention_FewPhotos_ViewsWithoutContacts_ExpiringSoon()
    {
        var fewPhotos = await AddListingAsync(_owner, photos: 1);
        var ignored = await AddListingAsync(_owner);
        var fine = await AddListingAsync(_owner);
        Day(ignored, Today, views: 25, reveals: 0);
        Day(fine, Today, views: 25, reveals: 2);
        await _db.SaveChangesAsync();

        var stats = (await StatsAsync(_owner))!;

        Assert.Equal(
            [(fewPhotos.Id, "FewPhotos"), (ignored.Id, "NoContacts")],
            stats.NeedsAttention.Select(a => (a.ListingId, a.Reason)));

        _clock.Now = fine.ExpiresAt!.Value.AddDays(-2);
        var later = (await StatsAsync(_owner, from: Today, to: Today))!;
        Assert.Contains(later.NeedsAttention, a => a.ListingId == fine.Id && a.Reason == "ExpiringSoon");
    }

    [Fact]
    public async Task NeedsAttention_IsOrdered_FewPhotos_ThenNoContacts_ThenExpiringSoon()
    {
        // Added in the reverse order on purpose; the reasons' names sort alphabetically the other way
        // (ExpiringSoon < FewPhotos < NoContacts), which is the bug this guards against.
        var expiring = await AddListingAsync(_owner);
        var ignored = await AddListingAsync(_owner);
        var fewPhotos = await AddListingAsync(_owner, photos: 1);
        Day(expiring, Today, views: 30, reveals: 1);
        Day(ignored, Today, views: 25, reveals: 0);
        await _db.SaveChangesAsync();
        _clock.Now = expiring.ExpiresAt!.Value.AddDays(-2);

        var stats = (await StatsAsync(_owner, from: Today, to: Today))!;

        Assert.Equal(
            ["FewPhotos", "NoContacts", "ExpiringSoon"],
            stats.NeedsAttention.Select(a => a.Reason));
        Assert.Equal([fewPhotos.Id, ignored.Id, expiring.Id], stats.NeedsAttention.Select(a => a.ListingId));
    }

    [Fact]
    public async Task AnAgent_SeesOnlyTheirOwnListings_AndNotTheAgencyPage()
    {
        var agent = await AddMemberAsync(AgencyRole.Agent);
        var mine = await AddListingAsync(agent);
        var theOwners = await AddListingAsync(_owner);
        Day(mine, Today, views: 7, reveals: 1);
        Day(theOwners, Today, views: 70, reveals: 9);
        _db.AgencyPhoneReveals.Add(AgencyPhoneReveal.Create(_agency.Id, Today, "v1", Now));
        await _db.SaveChangesAsync();

        var stats = (await StatsAsync(agent))!;

        Assert.Equal(7, stats.Totals.Views);
        Assert.Equal(1, stats.Totals.Contacts);
        Assert.Equal(0, stats.Totals.AgencyPhoneReveals);
    }

    [Fact]
    public async Task Outsiders_AreForbidden_AnUnknownAgencyIsNull()
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => StatsAsync(Guid.NewGuid()));
        Assert.NotNull(await StatsAsync(Guid.NewGuid(), isAdmin: true));
        Assert.Null(await new GetAgencyStatsHandler(_db, new FakeBlobStorageService(), _clock)
            .Handle(new GetAgencyStatsQuery(Guid.NewGuid(), _owner, false), CancellationToken.None));
    }

    [Fact]
    public void ThePeriod_IsInOrder_AndAtMostAYear()
    {
        var validator = new GetAgencyStatsValidator();

        Assert.False(validator.Validate(new GetAgencyStatsQuery(Guid.NewGuid(), Guid.NewGuid(), false, Today, Today.AddDays(-1))).IsValid);
        Assert.False(validator.Validate(new GetAgencyStatsQuery(Guid.NewGuid(), Guid.NewGuid(), false, Today.AddDays(-365), Today)).IsValid);
        Assert.True(validator.Validate(new GetAgencyStatsQuery(Guid.NewGuid(), Guid.NewGuid(), false, Today.AddDays(-364), Today)).IsValid);
    }
}
