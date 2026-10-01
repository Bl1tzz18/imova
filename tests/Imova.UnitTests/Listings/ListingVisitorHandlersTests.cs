using Imova.Application.Features.Listings.RecordListingView;
using Imova.Application.Features.Listings.RevealListingPhone;
using Imova.Application.Features.Listings.Visitors;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class ListingVisitorHandlersTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly ImovaDbContext _dbContext = TestDbContextFactory.Create();
    private readonly FakeListingCounters _counters = new();
    private readonly ManualTimeProvider _time = new(Start);
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _publisherId;

    public ListingVisitorHandlersTests()
    {
        _publisherId = ListingTestData.AddIndividualPublisher(_dbContext, _ownerId).Id;
    }

    private async Task<Listing> AddAsync(ListingStatus status = ListingStatus.Active, ListingContact? contact = null)
    {
        var listing = ListingTestData.AddListing(_dbContext, _publisherId, contact: contact ?? TestContacts.Self).MoveTo(status);
        await _dbContext.SaveChangesAsync();
        return listing;
    }

    private Task ViewAsync(Guid listingId, Guid? userId = null, string? visitorId = "visitor-1111") =>
        new RecordListingViewHandler(_dbContext, _counters, _time).Handle(new RecordListingViewCommand(listingId, userId, visitorId), CancellationToken.None);

    private Task<Imova.Contracts.Listings.ListingPhoneDto?> RevealAsync(Guid listingId, Guid? userId = null, bool isAdmin = false, string? visitorId = "visitor-1111") =>
        new RevealListingPhoneHandler(_dbContext, _counters, _time).Handle(new RevealListingPhoneCommand(listingId, userId, isAdmin, visitorId), CancellationToken.None);

    [Fact]
    public async Task AView_CountsOncePerVisitorPer24Hours()
    {
        var listing = await AddAsync();

        await ViewAsync(listing.Id);
        await ViewAsync(listing.Id);
        await ViewAsync(listing.Id, visitorId: "visitor-2222");
        _time.Advance(ListingVisitorMark.CountOncePer + TimeSpan.FromMinutes(1));
        await ViewAsync(listing.Id);

        Assert.Equal(3, _counters.Counted.Count(c => c.Counter == ListingCounter.View));
    }

    [Fact]
    public async Task TheOwnersOwnViews_AnInactiveListing_AndAnUnidentifiableVisitor_AreNotCounted()
    {
        var active = await AddAsync();
        var draft = await AddAsync(ListingStatus.Draft);

        await ViewAsync(active.Id, userId: _ownerId);
        await ViewAsync(draft.Id);
        await ViewAsync(active.Id, visitorId: null);
        await ViewAsync(Guid.NewGuid());

        Assert.Empty(_counters.Counted);
    }

    [Fact]
    public async Task RevealingThePhone_GivesTheNumber_AndCountsTheVisitorOnce()
    {
        var listing = await AddAsync();

        var first = await RevealAsync(listing.Id);
        var again = await RevealAsync(listing.Id);

        Assert.Equal("+373 69 111 222", first!.Phone);
        Assert.Equal("+373 69 111 222", again!.Phone);
        Assert.Single(_counters.Counted, c => c.Counter == ListingCounter.PhoneReveal);
    }

    [Fact]
    public async Task AHiddenNumber_IsOnlyForTheOwnerAndAdmins_AndTheyArentCounted()
    {
        var listing = await AddAsync(contact: TestContacts.HiddenPhone);

        Assert.Null(await RevealAsync(listing.Id));
        Assert.Equal("+373 69 555 666", (await RevealAsync(listing.Id, userId: _ownerId))!.Phone);
        Assert.Equal("+373 69 555 666", (await RevealAsync(listing.Id, userId: Guid.NewGuid(), isAdmin: true))!.Phone);
        Assert.Single(_counters.Counted); // the admin — only the owner is never counted
    }

    [Fact]
    public async Task AListingThatIsntActive_GivesNoNumber()
    {
        var draft = await AddAsync(ListingStatus.Draft);
        var expired = await AddAsync(ListingStatus.Expired);

        Assert.Null(await RevealAsync(draft.Id));
        Assert.Null(await RevealAsync(expired.Id));
        Assert.Null(await RevealAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Cleanup_DeletesMarksOlderThanTwoDays()
    {
        var listing = await AddAsync();
        var marks = typeof(ListingVisitorMark);
        void AddMark(string visitor, DateTimeOffset at)
        {
            var mark = (ListingVisitorMark)Activator.CreateInstance(marks, nonPublic: true)!;
            marks.GetProperty(nameof(ListingVisitorMark.ListingId))!.SetValue(mark, listing.Id);
            marks.GetProperty(nameof(ListingVisitorMark.Counter))!.SetValue(mark, ListingCounter.View);
            marks.GetProperty(nameof(ListingVisitorMark.VisitorHash))!.SetValue(mark, visitor);
            marks.GetProperty(nameof(ListingVisitorMark.CountedAt))!.SetValue(mark, at);
            _dbContext.ListingVisitorMarks.Add(mark);
        }

        AddMark("old", Start - ListingVisitorMark.KeepFor - TimeSpan.FromHours(1));
        AddMark("recent", Start - TimeSpan.FromHours(30));
        await _dbContext.SaveChangesAsync();

        var deleted = await new ListingVisitorMarkCleanup(_dbContext, _time).RunAsync(CancellationToken.None);

        Assert.Equal(1, deleted);
        Assert.Equal(["recent"], _dbContext.ListingVisitorMarks.Select(m => m.VisitorHash).ToList());
    }
}
