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

        Assert.Null(report.ResolvedBy);
    }

    [Fact]
    public async Task Resolving_MovesAReportFromActiveToResolved_AndReopeningMovesItBack()
    {
        var fixture = new MessagingFixture();
        var admin = fixture.AddUser("Admin IMOVA", "admin@example.com");
        var started = await fixture.StartAsync();
        await new ReportConversationHandler(fixture.Db, fixture.Clock).Handle(
            new ReportConversationCommand(fixture.Seller.Id, started!.ConversationId, ReportReason.Spam, null), CancellationToken.None);
        var reports = new GetMessagingReportsHandler(fixture.Db);
        var setResolved = new SetMessagingReportResolvedHandler(fixture.Db, fixture.Clock);
        var reportId = Assert.Single(await reports.Handle(new GetMessagingReportsQuery(true), CancellationToken.None)).Id;

        fixture.Clock.Advance(TimeSpan.FromHours(2));
        Assert.True(await setResolved.Handle(new SetMessagingReportResolvedCommand(true, admin.Id, reportId, true), CancellationToken.None));

        Assert.Empty(await reports.Handle(new GetMessagingReportsQuery(true, Resolved: false), CancellationToken.None));
        var resolved = Assert.Single(await reports.Handle(new GetMessagingReportsQuery(true, Resolved: true), CancellationToken.None));
        Assert.Equal(fixture.Clock.Now, resolved.ResolvedAt);
        Assert.Equal("Admin IMOVA", resolved.ResolvedBy!.DisplayName);

        Assert.True(await setResolved.Handle(new SetMessagingReportResolvedCommand(true, admin.Id, reportId, false), CancellationToken.None));
        var active = Assert.Single(await reports.Handle(new GetMessagingReportsQuery(true), CancellationToken.None));
        Assert.Null(active.ResolvedAt);
        Assert.Null(active.ResolvedBy);
        Assert.Empty(await reports.Handle(new GetMessagingReportsQuery(true, Resolved: true), CancellationToken.None));

        Assert.False(await setResolved.Handle(new SetMessagingReportResolvedCommand(true, admin.Id, Guid.NewGuid(), true), CancellationToken.None));
    }

    [Fact]
    public async Task Resolving_AFlaggedMessage_MovesItToResolved_AndReopeningMovesItBack()
    {
        var fixture = new MessagingFixture();
        var admin = fixture.AddUser("Admin IMOVA", "admin@example.com");
        var started = await fixture.StartAsync(body: "Plata în avans prin Western Union, vă rog");
        var clean = await fixture.SendAsync(fixture.Seller.Id, started!.ConversationId, "Bună ziua!");
        var flagged = new GetFlaggedMessagesHandler(fixture.Db);
        var setResolved = new SetFlaggedMessageResolvedHandler(fixture.Db, fixture.Clock);
        var messageId = started.Message.Id;

        Assert.True(await setResolved.Handle(new SetFlaggedMessageResolvedCommand(true, admin.Id, messageId, true), CancellationToken.None));

        Assert.Empty(await flagged.Handle(new GetFlaggedMessagesQuery(true, Resolved: false), CancellationToken.None));
        var resolved = Assert.Single(await flagged.Handle(new GetFlaggedMessagesQuery(true, Resolved: true), CancellationToken.None));
        Assert.Equal(messageId, resolved.Message.Id);
        Assert.Equal(fixture.Clock.Now, resolved.ResolvedAt);
        Assert.Equal(admin.Id, resolved.ResolvedBy!.Id);

        Assert.True(await setResolved.Handle(new SetFlaggedMessageResolvedCommand(true, admin.Id, messageId, false), CancellationToken.None));
        Assert.Null(Assert.Single(await flagged.Handle(new GetFlaggedMessagesQuery(true), CancellationToken.None)).ResolvedAt);

        // A message that was never flagged can't be "resolved".
        Assert.False(await setResolved.Handle(new SetFlaggedMessageResolvedCommand(true, admin.Id, clean!.Id, true), CancellationToken.None));
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
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            new SetMessagingReportResolvedHandler(fixture.Db, fixture.Clock)
                .Handle(new SetMessagingReportResolvedCommand(false, fixture.Visitor.Id, Guid.NewGuid(), true), CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            new SetFlaggedMessageResolvedHandler(fixture.Db, fixture.Clock)
                .Handle(new SetFlaggedMessageResolvedCommand(false, fixture.Visitor.Id, Guid.NewGuid(), true), CancellationToken.None));
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
