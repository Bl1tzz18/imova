using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Admins.GetAdminSummary;
using Imova.Contracts.Admins;
using Imova.Domain.Listings;
using Imova.Domain.Messaging;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Admins;

// What's waiting for an admin: only open items count, each in its own area.
public class GetAdminSummaryHandlerTests
{
    private readonly MessagingFixture _f = new();

    private Listing AddListing(ListingStatus status)
    {
        var publisher = ListingTestData.AddIndividualPublisher(_f.Db, _f.Seller.Id);
        var listing = ListingTestData.AddListing(_f.Db, publisher.Id).MoveTo(status);
        _f.Db.SaveChanges();
        return listing;
    }

    [Fact]
    public async Task CountsOnlyWhatIsStillWaiting()
    {
        AddListing(ListingStatus.PendingReview);
        AddListing(ListingStatus.PendingReview);
        AddListing(ListingStatus.Rejected);

        // Two open reports on one listing count as one reported listing; a dismissed one doesn't count.
        var reported = AddListing(ListingStatus.Active);
        var handled = AddListing(ListingStatus.Active);
        var otherReporter = _f.AddUser("Victor Lungu", "victor@example.com").Id;
        _f.Db.ListingReports.AddRange(
            ListingReport.Create(reported, _f.Visitor.Id, ListingReportReason.Fraud, null, _f.Clock.Now),
            ListingReport.Create(reported, otherReporter, ListingReportReason.Duplicate, null, _f.Clock.Now));
        var dismissed = ListingReport.Create(handled, _f.Visitor.Id, ListingReportReason.Fraud, null, _f.Clock.Now);
        dismissed.Resolve(ListingReportOutcome.Dismissed, _f.Seller.Id, null, _f.Clock.Now);
        _f.Db.ListingReports.Add(dismissed);

        var conversation = Conversation.Start(_f.Listing.Id, _f.Visitor.Id, _f.Seller.Id, _f.Clock.Now);
        _f.Db.Conversations.Add(conversation);
        _f.Db.ConversationReports.Add(ConversationReport.Create(conversation, _f.Seller.Id, ReportReason.Spam, null, _f.Clock.Now));
        var resolvedReport = ConversationReport.Create(conversation, _f.Visitor.Id, ReportReason.Spam, null, _f.Clock.Now);
        resolvedReport.Resolve(_f.Seller.Id, _f.Clock.Now);
        _f.Db.ConversationReports.Add(resolvedReport);

        var resolvedFlag = conversation.AddMessage(_f.Visitor.Id, "plata in avans", [], true, "advance", _f.Clock.Now);
        resolvedFlag.ResolveFlag(_f.Seller.Id, _f.Clock.Now);
        _f.Db.Messages.AddRange(
            conversation.AddMessage(_f.Visitor.Id, "trimite banii pe card", [], true, "card", _f.Clock.Now),
            conversation.AddMessage(_f.Visitor.Id, "bună ziua", [], false, null, _f.Clock.Now),
            resolvedFlag);
        await _f.Db.SaveChangesAsync();

        var summary = await new GetAdminSummaryHandler(_f.Db).Handle(new GetAdminSummaryQuery(true), CancellationToken.None);

        Assert.Equal(
            new AdminSummaryDto(PendingReviewListings: 2, ReportedListings: 1, OpenConversationReports: 1, FlaggedMessages: 1),
            summary);
    }

    [Fact]
    public async Task NonAdmins_AreForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            new GetAdminSummaryHandler(_f.Db).Handle(new GetAdminSummaryQuery(false), CancellationToken.None));
    }
}
