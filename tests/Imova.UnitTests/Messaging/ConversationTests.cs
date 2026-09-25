using Imova.Domain.Messaging;

namespace Imova.UnitTests.Messaging;

public class ConversationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);
    private readonly Guid _visitor = Guid.NewGuid();
    private readonly Guid _seller = Guid.NewGuid();

    private Conversation New() => Conversation.Start(Guid.NewGuid(), _visitor, _seller, T0);

    [Fact]
    public void Start_AboutYourOwnListing_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => Conversation.Start(Guid.NewGuid(), _seller, _seller, T0));
    }

    [Fact]
    public void OtherParticipant_IsTheOtherOne_AndOutsidersAreRejected()
    {
        var conversation = New();

        Assert.Equal(_seller, conversation.OtherParticipant(_visitor));
        Assert.Equal(_visitor, conversation.OtherParticipant(_seller));
        Assert.Throws<InvalidOperationException>(() => conversation.OtherParticipant(Guid.NewGuid()));
    }

    [Fact]
    public void Archive_IsPerParticipant_AndANewMessageUnarchivesForBoth()
    {
        var conversation = New();
        conversation.Archive(_visitor);

        Assert.True(conversation.IsArchivedFor(_visitor));
        Assert.False(conversation.IsArchivedFor(_seller));

        conversation.Archive(_seller);
        conversation.AddMessage(_seller, "Revin", [], false, null, T0.AddMinutes(1));

        Assert.False(conversation.IsArchivedFor(_visitor));
        Assert.False(conversation.IsArchivedFor(_seller));
        Assert.Equal(T0.AddMinutes(1), conversation.LastMessageAt);
    }

    [Fact]
    public void ShouldEmail_OnlyForTheFirstUnreadMessage_AndNotAgainWithinTenMinutes()
    {
        var conversation = New();

        Assert.True(conversation.ShouldEmail(_seller, unreadBeforeThisMessage: 0, T0));
        Assert.False(conversation.ShouldEmail(_seller, unreadBeforeThisMessage: 1, T0));

        conversation.RecordEmailSent(_seller, T0);
        Assert.False(conversation.ShouldEmail(_seller, 0, T0.AddMinutes(9)));
        Assert.True(conversation.ShouldEmail(_seller, 0, T0.AddMinutes(10)));
        // The cooldown is per recipient.
        Assert.True(conversation.ShouldEmail(_visitor, 0, T0.AddMinutes(1)));
    }

    [Fact]
    public void MessageStatus_GoesSentDeliveredRead_AndNeverBack()
    {
        var message = New().AddMessage(_visitor, "Bună", [], false, null, T0);
        Assert.Equal(MessageStatus.Sent, message.Status);

        Assert.True(message.MarkDelivered(T0.AddSeconds(5)));
        Assert.Equal(MessageStatus.Delivered, message.Status);
        Assert.False(message.MarkDelivered(T0.AddSeconds(9)));
        Assert.Equal(T0.AddSeconds(5), message.DeliveredAt);

        Assert.True(message.MarkRead(T0.AddMinutes(1)));
        Assert.Equal(MessageStatus.Read, message.Status);
        Assert.False(message.MarkRead(T0.AddMinutes(2)));
        Assert.False(message.MarkDelivered(T0.AddMinutes(3)));
        Assert.Equal(MessageStatus.Read, message.Status);
    }

    [Fact]
    public void MarkRead_OnASentMessage_AlsoMarksItDelivered()
    {
        var message = New().AddMessage(_visitor, "Bună", [], false, null, T0);

        message.MarkRead(T0.AddMinutes(1));

        Assert.Equal(T0.AddMinutes(1), message.DeliveredAt);
    }

    [Fact]
    public void Message_NeedsTextOrAnImage_AndRespectsTheLimits()
    {
        var conversation = New();
        MessageAttachment Image() => new($"messages/{_visitor}/{Guid.NewGuid()}.png", "image/png", 10);

        Assert.Throws<ArgumentException>(() => conversation.AddMessage(_visitor, "   ", [], false, null, T0));
        Assert.Throws<ArgumentException>(() => conversation.AddMessage(_visitor, new string('a', 2001), [], false, null, T0));
        Assert.Throws<ArgumentException>(() =>
            conversation.AddMessage(_visitor, "x", Enumerable.Range(0, 6).Select(_ => Image()), false, null, T0));

        var imageOnly = conversation.AddMessage(_visitor, "", [Image(), Image()], false, null, T0);
        Assert.Equal([0, 1], imageOnly.Attachments.Select(a => a.SortOrder));
        Assert.All(imageOnly.Attachments, a => Assert.Equal(imageOnly.Id, a.MessageId));
    }

    [Fact]
    public void Report_OtherNeedsDetails_AndOnlyParticipantsCanReport()
    {
        var conversation = New();

        Assert.Throws<ArgumentException>(() => ConversationReport.Create(conversation, _visitor, ReportReason.Other, " ", T0));
        Assert.Throws<InvalidOperationException>(() => ConversationReport.Create(conversation, Guid.NewGuid(), ReportReason.Spam, null, T0));

        var report = ConversationReport.Create(conversation, _seller, ReportReason.Fraud, "  Cere avans  ", T0);
        Assert.Equal("Cere avans", report.Details);
        Assert.Null(report.ResolvedAt);
    }

    [Fact]
    public void Report_ResolveKeepsTheFirstAdmin_AndReopenClearsIt()
    {
        var report = ConversationReport.Create(New(), _visitor, ReportReason.Spam, null, T0);
        var firstAdmin = Guid.NewGuid();

        report.Resolve(firstAdmin, T0.AddHours(1));
        report.Resolve(Guid.NewGuid(), T0.AddHours(2));
        Assert.Equal((firstAdmin, T0.AddHours(1)), (report.ResolvedByUserId!.Value, report.ResolvedAt!.Value));

        report.Reopen();
        Assert.False(report.IsResolved);
        Assert.Null(report.ResolvedByUserId);
    }

    [Fact]
    public void FlagResolution_OnlyForFlaggedMessages()
    {
        var conversation = New();
        var flagged = conversation.AddMessage(_visitor, "Western Union", [], true, "Money transfer service", T0);
        var clean = conversation.AddMessage(_visitor, "Bună", [], false, null, T0);
        var admin = Guid.NewGuid();

        flagged.ResolveFlag(admin, T0.AddHours(1));
        Assert.Equal(admin, flagged.FlagResolvedByUserId);
        Assert.Throws<InvalidOperationException>(() => clean.ResolveFlag(admin, T0));

        flagged.ReopenFlag();
        Assert.Null(flagged.FlagResolvedAt);
        Assert.True(flagged.IsFlagged);
    }

    [Fact]
    public void UserBlock_CantBlockYourself()
    {
        Assert.Throws<ArgumentException>(() => new UserBlock(_seller, _seller, T0));
    }
}
