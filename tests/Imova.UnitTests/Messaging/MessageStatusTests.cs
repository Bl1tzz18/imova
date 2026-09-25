using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Messaging;

public class MessageStatusTests
{
    [Fact]
    public async Task UnreadCount_CountsOnlyMessagesSentToMe_AcrossConversations()
    {
        var fixture = new MessagingFixture();
        var other = ListingTestData.AddListing(fixture.Db, fixture.Listing.PublisherId).MoveTo(Imova.Domain.Listings.ListingStatus.Active);
        await fixture.Db.SaveChangesAsync();
        var first = await fixture.StartAsync();
        await fixture.SendAsync(fixture.Visitor.Id, first!.ConversationId);
        await fixture.StartAsync(listingId: other.Id);

        Assert.Equal(3, await fixture.UnreadAsync(fixture.Seller.Id));
        Assert.Equal(0, await fixture.UnreadAsync(fixture.Visitor.Id));
        Assert.Equal(2, (await fixture.InboxAsync(fixture.Seller.Id)).Single(c => c.Id == first.ConversationId).UnreadCount);
    }

    [Fact]
    public async Task MarkRead_ReadsTheConversation_TellsTheSender_AndUpdatesTheBadge()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();
        await fixture.SendAsync(fixture.Visitor.Id, started!.ConversationId);

        Assert.True(await fixture.MarkReadAsync(fixture.Seller.Id, started.ConversationId));

        Assert.Equal(0, await fixture.UnreadAsync(fixture.Seller.Id));
        var (change, senderUserId) = Assert.Single(fixture.Realtime.StatusChanges);
        Assert.Equal(fixture.Visitor.Id, senderUserId);
        Assert.Equal("Read", change.Status);
        Assert.Equal(2, change.MessageIds.Count);
        Assert.Equal((fixture.Seller.Id, 0), fixture.Realtime.UnreadCounts.Last());
        Assert.All(await fixture.Db.Messages.ToListAsync(), m => Assert.NotNull(m.ReadAt));
    }

    [Fact]
    public async Task MarkRead_DoesNotTouchMyOwnMessages()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();

        await fixture.MarkReadAsync(fixture.Visitor.Id, started!.ConversationId);

        Assert.Null((await fixture.Db.Messages.SingleAsync()).ReadAt);
        Assert.Empty(fixture.Realtime.StatusChanges);
    }

    [Fact]
    public async Task MarkRead_ByAnOutsider_IsNotFound()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();

        Assert.False(await fixture.MarkReadAsync(Guid.NewGuid(), started!.ConversationId));
    }

    [Fact]
    public async Task MarkDelivered_OnceTheRecipientShowsUp_AndOnlyOnce()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();
        Assert.Equal("Sent", started!.Message.Status);

        Assert.Equal(1, await fixture.MarkDeliveredAsync(fixture.Seller.Id));
        Assert.Equal(0, await fixture.MarkDeliveredAsync(fixture.Seller.Id));
        // The sender's own device doesn't "deliver" their message.
        Assert.Equal(0, await fixture.MarkDeliveredAsync(fixture.Visitor.Id));

        var (change, senderUserId) = Assert.Single(fixture.Realtime.StatusChanges);
        Assert.Equal((fixture.Visitor.Id, "Delivered"), (senderUserId, change.Status));
        Assert.Equal("Delivered", (await fixture.ThreadAsync(fixture.Visitor.Id, started.ConversationId))!.Messages.Single().Status);
    }

    [Fact]
    public async Task StatusProgresses_SentThenDeliveredThenRead()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();
        string Status() => fixture.Db.Messages.AsNoTracking().Single().ReadAt is not null ? "Read"
            : fixture.Db.Messages.AsNoTracking().Single().DeliveredAt is not null ? "Delivered" : "Sent";

        Assert.Equal("Sent", Status());
        await fixture.MarkDeliveredAsync(fixture.Seller.Id);
        Assert.Equal("Delivered", Status());
        await fixture.MarkReadAsync(fixture.Seller.Id, started!.ConversationId);
        Assert.Equal("Read", Status());
        Assert.Equal(["Delivered", "Read"], fixture.Realtime.StatusChanges.Select(s => s.Change.Status));
    }
}
