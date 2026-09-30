using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Listings;
using Imova.Application.Features.Listings.ReportListing;
using Imova.Domain.Listings;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Listings;

// The visitor (Maria) reports the seller's (Ion's) Active listing — see MessagingFixture.
public class ReportListingHandlerTests
{
    private readonly MessagingFixture _f = new();
    private readonly ListingReportOptions _options = new() { MaxPerDay = 3 };

    private Task<Imova.Contracts.Listings.ReportListingResultDto?> ReportAsync(
        Guid? userId = null, Guid? listingId = null, ListingReportReason reason = ListingReportReason.Fraud, string? details = null) =>
        new ReportListingHandler(_f.Db, _f.Clock, _options)
            .Handle(new ReportListingCommand(userId ?? _f.Visitor.Id, listingId ?? _f.Listing.Id, reason, details), CancellationToken.None);

    private Listing AddActiveListing()
    {
        var publisher = ListingTestData.AddIndividualPublisher(_f.Db, _f.Seller.Id);
        var listing = ListingTestData.AddListing(_f.Db, publisher.Id).MoveTo(ListingStatus.Active);
        _f.Db.SaveChanges();
        return listing;
    }

    [Fact]
    public async Task Report_StoresAnOpenReport()
    {
        var result = await ReportAsync(reason: ListingReportReason.Fraud, details: "Cere avans înainte de vizionare.");

        Assert.False(result!.Amended);
        var report = await _f.Db.ListingReports.SingleAsync();
        Assert.Equal(_f.Listing.Id, report.ListingId);
        Assert.Equal(_f.Visitor.Id, report.ReporterUserId);
        Assert.Equal(ListingReportReason.Fraud, report.Reason);
        Assert.Equal("Cere avans înainte de vizionare.", report.Details);
        Assert.Equal(_f.Clock.Now, report.CreatedAt);
        Assert.False(report.IsResolved);
    }

    [Fact]
    public async Task ReportingAgainWhileOpen_AmendsTheSameReport()
    {
        await ReportAsync(reason: ListingReportReason.WrongInformation);
        _f.Clock.Advance(TimeSpan.FromMinutes(10));

        var result = await ReportAsync(reason: ListingReportReason.Fraud, details: "De fapt e o țeapă.");

        Assert.True(result!.Amended);
        var report = await _f.Db.ListingReports.SingleAsync();
        Assert.Equal(ListingReportReason.Fraud, report.Reason);
        Assert.Equal("De fapt e o țeapă.", report.Details);
        Assert.Equal(_f.Clock.Now, report.UpdatedAt);
    }

    [Fact]
    public async Task ReportingAgainAfterADecision_OpensANewReport()
    {
        await ReportAsync();
        var first = await _f.Db.ListingReports.SingleAsync();
        first.Resolve(ListingReportOutcome.Dismissed, Guid.NewGuid(), null, _f.Clock.Now);
        await _f.Db.SaveChangesAsync();

        var result = await ReportAsync();

        Assert.False(result!.Amended);
        Assert.Equal(2, await _f.Db.ListingReports.CountAsync());
    }

    [Fact]
    public async Task ReportingYourOwnListing_IsRejectedWithACode()
    {
        var error = await Assert.ThrowsAsync<ValidationException>(() => ReportAsync(userId: _f.Seller.Id));

        Assert.Equal(ErrorCodes.ReportOwnListing, Assert.Single(error.Errors).ErrorCode);
        Assert.Empty(_f.Db.ListingReports);
    }

    [Fact]
    public async Task AnUnknownListing_IsNotFound()
    {
        Assert.Null(await ReportAsync(listingId: Guid.NewGuid()));
    }

    [Fact]
    public async Task AListingThatIsNoLongerPublic_IsNotFound()
    {
        var listing = await _f.Db.Listings.SingleAsync(l => l.Id == _f.Listing.Id);
        listing.MarkAsRented();
        await _f.Db.SaveChangesAsync();

        Assert.Null(await ReportAsync());
        Assert.Empty(_f.Db.ListingReports);
    }

    [Fact]
    public async Task TheDailyLimit_StopsNewReports_ThenLiftsAfter24Hours()
    {
        var listings = Enumerable.Range(0, 4).Select(_ => AddActiveListing()).ToList();
        foreach (var listing in listings.Take(3))
        {
            await ReportAsync(listingId: listing.Id);
            _f.Clock.Advance(TimeSpan.FromHours(1));
        }

        var error = await Assert.ThrowsAsync<TooManyRequestsException>(() => ReportAsync(listingId: listings[3].Id));
        Assert.Equal(ErrorCodes.ReportLimitReached, error.Code);
        Assert.Equal(3, error.Params!["max"]);

        // The first report leaves the 24-hour window.
        _f.Clock.Advance(TimeSpan.FromHours(22));
        Assert.False((await ReportAsync(listingId: listings[3].Id))!.Amended);
    }

    [Fact]
    public async Task AmendingAtTheLimit_IsStillAllowed()
    {
        var listings = Enumerable.Range(0, 3).Select(_ => AddActiveListing()).ToList();
        foreach (var listing in listings)
        {
            await ReportAsync(listingId: listing.Id);
        }

        var result = await ReportAsync(listingId: listings[0].Id, reason: ListingReportReason.Duplicate);

        Assert.True(result!.Amended);
    }

    [Fact]
    public void Validator_RequiresDetailsForOther_AndCapsTheirLength()
    {
        var validator = new ReportListingValidator();

        var missing = validator.Validate(new ReportListingCommand(Guid.NewGuid(), Guid.NewGuid(), ListingReportReason.Other, " "));
        var tooLong = validator.Validate(new ReportListingCommand(
            Guid.NewGuid(), Guid.NewGuid(), ListingReportReason.Fraud, new string('a', ListingReport.MaxDetailsLength + 1)));
        var unknown = validator.Validate(new ReportListingCommand(Guid.NewGuid(), Guid.NewGuid(), (ListingReportReason)42, null));
        var fine = validator.Validate(new ReportListingCommand(Guid.NewGuid(), Guid.NewGuid(), ListingReportReason.NoLongerAvailable, null));

        Assert.Equal(ErrorCodes.ReportDetailsRequired, Assert.Single(missing.Errors).ErrorCode);
        Assert.False(tooLong.IsValid);
        Assert.False(unknown.IsValid);
        Assert.True(fine.IsValid);
    }
}
