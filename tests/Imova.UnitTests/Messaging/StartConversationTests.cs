using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Domain.Listings;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Messaging;

public class StartConversationTests
{
    [Fact]
    public async Task Conversation_AlwaysGoesToThePublisher_EvenWhenTheContactIsAnotherPerson()
    {
        var fixture = new MessagingFixture(TestContacts.Other);

        var result = await fixture.StartAsync();

        var conversation = await fixture.Db.Conversations.SingleAsync();
        Assert.Equal(fixture.Seller.Id, conversation.PublisherUserId);
        Assert.Equal(fixture.Visitor.Id, conversation.InitiatorUserId);
        Assert.Equal(fixture.Seller.Id, Assert.Single(fixture.Realtime.Created).RecipientUserId);
        Assert.False(result!.Reused);
    }

    [Fact]
    public async Task StartingAgain_ReusesTheSameConversation()
    {
        var fixture = new MessagingFixture();

        var first = await fixture.StartAsync(body: "Bună ziua!");
        var second = await fixture.StartAsync(body: "Revin cu o întrebare");

        Assert.Equal(first!.ConversationId, second!.ConversationId);
        Assert.True(second.Reused);
        Assert.Single(await fixture.Db.Conversations.ToListAsync());
        Assert.Equal(2, await fixture.Db.Messages.CountAsync());
    }

    [Fact]
    public async Task StartingAgain_BringsAnArchivedConversationBack()
    {
        var fixture = new MessagingFixture();
        var first = await fixture.StartAsync();
        (await fixture.Db.Conversations.SingleAsync()).Archive(fixture.Visitor.Id);
        await fixture.Db.SaveChangesAsync();

        await fixture.StartAsync();

        Assert.Equal(first!.ConversationId, Assert.Single(await fixture.InboxAsync(fixture.Visitor.Id)).Id);
    }

    [Fact]
    public async Task AboutYourOwnListing_IsRejected()
    {
        var fixture = new MessagingFixture();

        await Assert.ThrowsAsync<ValidationException>(() => fixture.StartAsync(userId: fixture.Seller.Id));
    }

    [Fact]
    public async Task OnAListingThatIsNotActive_ReturnsNull()
    {
        var fixture = new MessagingFixture();
        var publisherId = fixture.Listing.PublisherId;
        var pending = ListingTestData.AddListing(fixture.Db, publisherId).MoveTo(ListingStatus.PendingReview);
        await fixture.Db.SaveChangesAsync();

        Assert.Null(await fixture.StartAsync(listingId: pending.Id));
        Assert.Null(await fixture.StartAsync(listingId: Guid.NewGuid()));
    }

    [Fact]
    public async Task RateLimit_AllowsTenNewConversationsPerHour_ThenRejects()
    {
        var fixture = new MessagingFixture();
        var listings = Enumerable.Range(0, 11)
            .Select(_ => ListingTestData.AddListing(fixture.Db, fixture.Listing.PublisherId).MoveTo(ListingStatus.Active))
            .ToList();
        await fixture.Db.SaveChangesAsync();

        for (var i = 0; i < 10; i++)
        {
            await fixture.StartAsync(listingId: listings[i].Id);
            fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        }

        var error = await Assert.ThrowsAsync<TooManyRequestsException>(() => fixture.StartAsync(listingId: listings[10].Id));
        Assert.Contains("10", error.Message);

        // Writing again in an existing conversation isn't a new one — never limited.
        Assert.True((await fixture.StartAsync(listingId: listings[0].Id))!.Reused);

        // An hour after the first one, a slot frees up.
        fixture.Clock.Advance(TimeSpan.FromMinutes(51));
        Assert.False((await fixture.StartAsync(listingId: listings[10].Id))!.Reused);
    }

    [Fact]
    public async Task ABlockedVisitor_CantStartOrReopenAConversation()
    {
        var fixture = new MessagingFixture();
        fixture.Db.UserBlocks.Add(new Imova.Domain.Messaging.UserBlock(fixture.Seller.Id, fixture.Visitor.Id, fixture.Clock.Now));
        await fixture.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => fixture.StartAsync());
        Assert.Empty(await fixture.Db.Conversations.ToListAsync());
    }
}
