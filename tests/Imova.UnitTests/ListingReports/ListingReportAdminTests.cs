using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.ListingReports.DismissListingReports;
using Imova.Application.Features.ListingReports.GetListingReportSummary;
using Imova.Application.Features.ListingReports.GetReportedListings;
using Imova.Application.Features.Listings;
using Imova.Application.Features.Listings.SuspendListing;
using Imova.Contracts.Common;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.ListingReports;

// The admin side: the queue of reported listings, and what suspending / dismissing does to it.
public class ListingReportAdminTests
{
    private readonly MessagingFixture _f = new();
    private readonly Guid _adminId;

    public ListingReportAdminTests()
    {
        _adminId = _f.AddUser("Ana Admin", "ana@imova.md").Id;
    }

    private Listing AddActiveListing()
    {
        var publisher = ListingTestData.AddIndividualPublisher(_f.Db, _f.Seller.Id);
        var listing = ListingTestData.AddListing(_f.Db, publisher.Id).MoveTo(ListingStatus.Active);
        _f.Db.SaveChanges();
        return listing;
    }

    private ListingReport Report(Listing listing, Guid reporterId, ListingReportReason reason = ListingReportReason.Fraud, string? details = null)
    {
        var report = ListingReport.Create(listing, reporterId, reason, details, _f.Clock.Now);
        _f.Db.ListingReports.Add(report);
        _f.Db.SaveChanges();
        _f.Clock.Advance(TimeSpan.FromMinutes(5));
        return report;
    }

    private Task<PagedResult<ReportedListingDto>> QueueAsync(bool resolved = false, bool isAdmin = true) =>
        new GetReportedListingsHandler(_f.Db, _f.Blobs)
            .Handle(new GetReportedListingsQuery(isAdmin, resolved), CancellationToken.None);

    private Task SuspendAsync(Guid listingId, string reason) =>
        new SuspendListingHandler(_f.Db, _f.Blobs, _f.Clock)
            .Handle(new SuspendListingCommand(listingId, true, _adminId, reason), CancellationToken.None);

    private Task DismissAsync(Guid listingId, string? note = null, bool isAdmin = true) =>
        new DismissListingReportsHandler(_f.Db, _f.Clock)
            .Handle(new DismissListingReportsCommand(isAdmin, _adminId, listingId, note), CancellationToken.None);

    [Fact]
    public async Task TheQueue_HasOneCasePerListing_MostReportedFirst()
    {
        var once = _f.Listing;
        var twice = AddActiveListing();
        var third = _f.AddUser("Victor Lungu", "victor@example.com");
        Report(once, _f.Visitor.Id);
        Report(twice, _f.Visitor.Id, ListingReportReason.WrongInformation, "Suprafața reală e 40 m².");
        Report(twice, third.Id, ListingReportReason.Fraud, "Cere avans.");

        var queue = await QueueAsync();

        Assert.Equal(2, queue.TotalCount);
        Assert.Collection(
            queue.Items,
            top =>
            {
                Assert.Equal(twice.Id, top.Listing.Id);
                Assert.Equal(2, top.ReportCount);
                Assert.Null(top.Resolution);
                // Newest first.
                Assert.Equal(["Victor Lungu", "Maria Rusu"], top.Reports.Select(r => r.Reporter.DisplayName));
                Assert.Equal("victor@example.com", top.Reports[0].Reporter.Email);
                Assert.Equal("Cere avans.", top.Reports[0].Details);
                Assert.Equal(["Fraud", "WrongInformation"], top.Reasons.Select(r => r.Reason).Order());
                Assert.True(top.FirstReportedAt < top.LastReportedAt);
            },
            next => Assert.Equal(once.Id, next.Listing.Id));
    }

    [Fact]
    public async Task EquallyReportedListings_OldestReportFirst()
    {
        var newer = AddActiveListing();
        Report(_f.Listing, _f.Visitor.Id);
        Report(newer, _f.Visitor.Id);

        var queue = await QueueAsync();

        Assert.Equal([_f.Listing.Id, newer.Id], queue.Items.Select(c => c.Listing.Id));
    }

    [Fact]
    public async Task TheReasons_AreCountedMostFrequentFirst()
    {
        foreach (var (name, reason) in new[] { ("A", ListingReportReason.Duplicate), ("B", ListingReportReason.Fraud), ("C", ListingReportReason.Fraud) })
        {
            Report(_f.Listing, _f.AddUser(name, $"{name}@example.com").Id, reason);
        }

        var reasons = (await QueueAsync()).Items.Single().Reasons;

        Assert.Equal([("Fraud", 2), ("Duplicate", 1)], reasons.Select(r => (r.Reason, r.Count)));
    }

    [Fact]
    public async Task EachReporter_ComesWithTheirRecord()
    {
        // Maria reported another listing before, and that report was dismissed.
        var earlier = AddActiveListing();
        Report(earlier, _f.Visitor.Id);
        await DismissAsync(earlier.Id);
        Report(_f.Listing, _f.Visitor.Id);

        var reporter = (await QueueAsync()).Items.Single().Reports.Single().Reporter;

        Assert.Equal(_f.Visitor.Id, reporter.UserId);
        Assert.Equal(2, reporter.ReportsFiled);
        Assert.Equal(1, reporter.ReportsDismissed);
        Assert.False(reporter.IsDeleted);
    }

    [Fact]
    public async Task AReportFromADeletedAccount_IsKept_AndShownAsDeleted()
    {
        var gone = _f.AddUser("Fost Utilizator", "gone@example.com");
        Report(_f.Listing, gone.Id);
        _f.Db.Users.Remove(gone);
        await _f.Db.SaveChangesAsync();

        var reporter = (await QueueAsync()).Items.Single().Reports.Single().Reporter;

        Assert.True(reporter.IsDeleted);
        Assert.Null(reporter.DisplayName);
        Assert.Null(reporter.Email);
    }

    [Fact]
    public async Task Suspending_ClosesEveryOpenReport_AsOneCaseInTheHistory()
    {
        Report(_f.Listing, _f.Visitor.Id);
        Report(_f.Listing, _f.AddUser("Victor Lungu", "victor@example.com").Id, ListingReportReason.Duplicate);

        await SuspendAsync(_f.Listing.Id, "Anunț fraudulos — cere avans.");

        Assert.Empty((await QueueAsync()).Items);
        var decided = Assert.Single((await QueueAsync(resolved: true)).Items);
        Assert.Equal(2, decided.ReportCount);
        Assert.Equal("ListingSuspended", decided.Resolution!.Outcome);
        Assert.Equal("Anunț fraudulos — cere avans.", decided.Resolution.Note);
        Assert.Equal("Ana Admin", decided.Resolution.ResolvedByName);
        Assert.Equal(_f.Clock.Now, decided.Resolution.ResolvedAt);
        Assert.Equal("Suspended", decided.Listing.Status);
    }

    [Fact]
    public async Task Suspending_AListingWithoutReports_JustSuspendsIt()
    {
        await SuspendAsync(_f.Listing.Id, "Duplicat.");

        Assert.Equal(ListingStatus.Suspended, (await _f.Db.Listings.SingleAsync(l => l.Id == _f.Listing.Id)).Status);
        Assert.Empty((await QueueAsync(resolved: true)).Items);
    }

    [Fact]
    public async Task Dismissing_ClosesTheReports_AndLeavesTheListingActive()
    {
        Report(_f.Listing, _f.Visitor.Id, ListingReportReason.NoLongerAvailable);

        await DismissAsync(_f.Listing.Id, "Am verificat cu proprietarul, e disponibil.");

        var decided = Assert.Single((await QueueAsync(resolved: true)).Items);
        Assert.Equal("Dismissed", decided.Resolution!.Outcome);
        Assert.Equal("Am verificat cu proprietarul, e disponibil.", decided.Resolution.Note);
        Assert.Equal("Active", decided.Listing.Status);
    }

    [Fact]
    public async Task DismissingWhenNothingIsOpen_SaysSo()
    {
        Report(_f.Listing, _f.Visitor.Id);
        await DismissAsync(_f.Listing.Id);

        var error = await Assert.ThrowsAsync<ValidationException>(() => DismissAsync(_f.Listing.Id));

        Assert.Equal(ErrorCodes.NoOpenReports, Assert.Single(error.Errors).ErrorCode);
    }

    [Fact]
    public async Task TwoDecisionsOnTheSameListing_AreTwoCases_NewestFirst()
    {
        Report(_f.Listing, _f.Visitor.Id);
        await DismissAsync(_f.Listing.Id, "Nimic în neregulă.");
        _f.Clock.Advance(TimeSpan.FromDays(1));
        Report(_f.Listing, _f.Visitor.Id, ListingReportReason.Fraud, "Acum cere bani în avans.");
        await SuspendAsync(_f.Listing.Id, "Fraudă confirmată.");

        var history = await QueueAsync(resolved: true);

        Assert.Equal(2, history.TotalCount);
        Assert.Equal(["ListingSuspended", "Dismissed"], history.Items.Select(c => c.Resolution!.Outcome));
        Assert.All(history.Items, c => Assert.Equal(1, c.ReportCount));
    }

    [Fact]
    public async Task TheSummary_CountsListingsAndOpenReports()
    {
        var other = AddActiveListing();
        Report(_f.Listing, _f.Visitor.Id);
        Report(_f.Listing, _f.AddUser("Victor Lungu", "victor@example.com").Id);
        Report(other, _f.Visitor.Id);
        var dismissed = AddActiveListing();
        Report(dismissed, _f.Visitor.Id);
        await DismissAsync(dismissed.Id);

        var summary = await new GetListingReportSummaryHandler(_f.Db)
            .Handle(new GetListingReportSummaryQuery(true), CancellationToken.None);

        Assert.Equal(new ListingReportSummaryDto(OpenListings: 2, OpenReports: 3), summary);
    }

    [Fact]
    public async Task NonAdmins_AreForbidden()
    {
        Report(_f.Listing, _f.Visitor.Id);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => QueueAsync(isAdmin: false));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => DismissAsync(_f.Listing.Id, isAdmin: false));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => new GetListingReportSummaryHandler(_f.Db)
            .Handle(new GetListingReportSummaryQuery(false), CancellationToken.None));
        Assert.False((await _f.Db.ListingReports.SingleAsync()).IsResolved);
    }

    [Fact]
    public async Task DeletingAListing_TakesItsReportsWithIt()
    {
        var other = AddActiveListing();
        Report(_f.Listing, _f.Visitor.Id);
        Report(other, _f.Visitor.Id);

        await ListingRemoval.RemoveAsync(_f.Db, [await _f.Db.Listings.SingleAsync(l => l.Id == _f.Listing.Id)], CancellationToken.None);
        await _f.Db.SaveChangesAsync();

        Assert.Equal(other.Id, (await _f.Db.ListingReports.SingleAsync()).ListingId);
    }
}
