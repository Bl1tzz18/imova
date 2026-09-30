using Imova.Domain.Listings;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class ListingReportTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

    private static Listing Active() => ListingTestData.NewListing().MoveTo(ListingStatus.Active);

    [Theory]
    [InlineData(ListingStatus.Draft)]
    [InlineData(ListingStatus.PendingReview)]
    [InlineData(ListingStatus.Suspended)]
    [InlineData(ListingStatus.Rented)]
    [InlineData(ListingStatus.Expired)]
    public void Create_ForAListingThatIsNotPublic_Throws(ListingStatus status)
    {
        var listing = ListingTestData.NewListing().MoveTo(status);

        Assert.Throws<InvalidOperationException>(() =>
            ListingReport.Create(listing, Guid.NewGuid(), ListingReportReason.Fraud, null, Now));
    }

    [Fact]
    public void Create_StartsOpen_WithTrimmedDetails()
    {
        var listing = Active();
        var reporter = Guid.NewGuid();

        var report = ListingReport.Create(listing, reporter, ListingReportReason.Fraud, "  Cere avans pe card.  ", Now);

        Assert.Equal(listing.Id, report.ListingId);
        Assert.Equal(reporter, report.ReporterUserId);
        Assert.Equal("Cere avans pe card.", report.Details);
        Assert.Equal(Now, report.CreatedAt);
        Assert.Equal(Now, report.UpdatedAt);
        Assert.False(report.IsResolved);
        Assert.Null(report.Outcome);
    }

    [Fact]
    public void Create_WithBlankDetails_StoresNone()
    {
        var report = ListingReport.Create(Active(), Guid.NewGuid(), ListingReportReason.Duplicate, "   ", Now);

        Assert.Null(report.Details);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Create_OtherWithoutDetails_Throws(string? details)
    {
        Assert.Throws<ArgumentException>(() =>
            ListingReport.Create(Active(), Guid.NewGuid(), ListingReportReason.Other, details, Now));
    }

    [Fact]
    public void Create_WithAnUnknownReason_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ListingReport.Create(Active(), Guid.NewGuid(), (ListingReportReason)99, null, Now));
    }

    [Fact]
    public void Amend_ReplacesTheReasonAndDetails_AndMovesUpdatedAt()
    {
        var report = ListingReport.Create(Active(), Guid.NewGuid(), ListingReportReason.WrongInformation, "Suprafața e greșită.", Now);

        report.Amend(ListingReportReason.Fraud, null, Now.AddHours(2));

        Assert.Equal(ListingReportReason.Fraud, report.Reason);
        Assert.Null(report.Details);
        Assert.Equal(Now, report.CreatedAt);
        Assert.Equal(Now.AddHours(2), report.UpdatedAt);
    }

    [Fact]
    public void Amend_AResolvedReport_Throws()
    {
        var report = ListingReport.Create(Active(), Guid.NewGuid(), ListingReportReason.Fraud, null, Now);
        report.Resolve(ListingReportOutcome.Dismissed, Guid.NewGuid(), null, Now);

        Assert.Throws<InvalidOperationException>(() => report.Amend(ListingReportReason.Duplicate, null, Now));
    }

    [Fact]
    public void Resolve_RecordsTheDecision_AndResolvingAgainKeepsTheFirst()
    {
        var report = ListingReport.Create(Active(), Guid.NewGuid(), ListingReportReason.Fraud, null, Now);
        var admin = Guid.NewGuid();

        report.Resolve(ListingReportOutcome.ListingSuspended, admin, "  Anunț fraudulos.  ", Now.AddHours(1));
        report.Resolve(ListingReportOutcome.Dismissed, Guid.NewGuid(), "Altceva", Now.AddHours(5));

        Assert.True(report.IsResolved);
        Assert.Equal(ListingReportOutcome.ListingSuspended, report.Outcome);
        Assert.Equal(admin, report.ResolvedByUserId);
        Assert.Equal(Now.AddHours(1), report.ResolvedAt);
        Assert.Equal("Anunț fraudulos.", report.ResolutionNote);
    }

    [Fact]
    public void Resolve_WithABlankNote_StoresNone()
    {
        var report = ListingReport.Create(Active(), Guid.NewGuid(), ListingReportReason.Fraud, null, Now);

        report.Resolve(ListingReportOutcome.Dismissed, Guid.NewGuid(), " ", Now);

        Assert.Null(report.ResolutionNote);
    }
}
