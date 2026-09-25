using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Messaging.Admin;
using Imova.Application.Features.Messaging.ReportConversation;
using Imova.Domain.Messaging;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Messaging;

public class ReportAndModerationTests
{
    [Fact]
    public async Task Report_IsListedForAdmins_WithWhoReportedWhom_UntilResolved()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();
        var reported = await new ReportConversationHandler(fixture.Db, fixture.Clock).Handle(
            new ReportConversationCommand(fixture.Seller.Id, started!.ConversationId, ReportReason.Fraud, "Cere bani în avans"),
            CancellationToken.None);
        Assert.True(reported);

        var reports = await new GetMessagingReportsHandler(fixture.Db).Handle(new GetMessagingReportsQuery(true), CancellationToken.None);
        var report = Assert.Single(reports);
        Assert.Equal("Fraud", report.Reason);
        Assert.Equal("Cere bani în avans", report.Details);
        Assert.Equal(fixture.Seller.Id, report.Reporter.Id);
        Assert.Equal(fixture.Visitor.Id, report.ReportedUser.Id);
        Assert.Equal(fixture.Listing.Title, report.Listing.Title);

        var conversation = await new GetConversationForAdminHandler(fixture.Db)
            .Handle(new GetConversationForAdminQuery(true, started.ConversationId), CancellationToken.None);
        Assert.Single(conversation!.Messages);

        await new ResolveMessagingReportHandler(fixture.Db, fixture.Clock)
            .Handle(new ResolveMessagingReportCommand(true, Guid.NewGuid(), report.Id), CancellationToken.None);
        Assert.Empty(await new GetMessagingReportsHandler(fixture.Db).Handle(new GetMessagingReportsQuery(true), CancellationToken.None));
        Assert.Single(await new GetMessagingReportsHandler(fixture.Db).Handle(new GetMessagingReportsQuery(true, IncludeResolved: true), CancellationToken.None));
    }

    [Fact]
    public async Task Report_ByAnOutsider_IsNotFound()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();

        Assert.False(await new ReportConversationHandler(fixture.Db, fixture.Clock).Handle(
            new ReportConversationCommand(Guid.NewGuid(), started!.ConversationId, ReportReason.Spam, null), CancellationToken.None));
    }

    [Fact]
    public void ReportValidator_OtherNeedsADescription()
    {
        var validator = new ReportConversationValidator();

        Assert.False(validator.Validate(new ReportConversationCommand(Guid.NewGuid(), Guid.NewGuid(), ReportReason.Other, " ")).IsValid);
        Assert.True(validator.Validate(new ReportConversationCommand(Guid.NewGuid(), Guid.NewGuid(), ReportReason.Other, "Insulte")).IsValid);
        Assert.True(validator.Validate(new ReportConversationCommand(Guid.NewGuid(), Guid.NewGuid(), ReportReason.Spam, null)).IsValid);
        Assert.False(validator.Validate(new ReportConversationCommand(Guid.NewGuid(), Guid.NewGuid(), (ReportReason)9, null)).IsValid);
    }

    [Fact]
    public async Task AdminEndpoints_RejectNonAdmins()
    {
        var fixture = new MessagingFixture();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            new GetMessagingReportsHandler(fixture.Db).Handle(new GetMessagingReportsQuery(false), CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            new SetMessagingBanHandler(fixture.Db).Handle(new SetMessagingBanCommand(false, fixture.Visitor.Id, true), CancellationToken.None));
    }

    [Fact]
    public async Task Ban_CanBeLifted()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();
        var ban = new SetMessagingBanHandler(fixture.Db);

        await ban.Handle(new SetMessagingBanCommand(true, fixture.Visitor.Id, true), CancellationToken.None);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => fixture.SendAsync(fixture.Visitor.Id, started!.ConversationId));

        await ban.Handle(new SetMessagingBanCommand(true, fixture.Visitor.Id, false), CancellationToken.None);
        Assert.NotNull(await fixture.SendAsync(fixture.Visitor.Id, started!.ConversationId));
        Assert.False(await ban.Handle(new SetMessagingBanCommand(true, Guid.NewGuid(), true), CancellationToken.None));
    }
}
