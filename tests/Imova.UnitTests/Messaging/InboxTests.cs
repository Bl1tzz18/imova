using Imova.Application.Features.Messaging.GetConversationIdForListing;
using Imova.Application.Features.Messaging.SetConversationArchived;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Messaging;

public class InboxTests
{
    [Fact]
    public async Task Inbox_ShowsEachSideTheOtherParticipantAndTheListing()
    {
        var fixture = new MessagingFixture();
        await fixture.StartAsync(body: "Bună ziua!");

        var visitorRow = Assert.Single(await fixture.InboxAsync(fixture.Visitor.Id));
        var sellerRow = Assert.Single(await fixture.InboxAsync(fixture.Seller.Id));

        // The visitor sees the publisher's name; the publisher sees the visitor's account name.
        Assert.Equal("Ion Popescu", visitorRow.OtherParticipant.DisplayName);
        Assert.Equal("Maria Rusu", sellerRow.OtherParticipant.DisplayName);
        Assert.True(visitorRow.IsInitiator);
        Assert.False(sellerRow.IsInitiator);
        Assert.Equal(fixture.Listing.Title, sellerRow.Listing.Title);
        Assert.Equal("Bună ziua!", sellerRow.LastMessage!.Body);
    }

    [Fact]
    public async Task Inbox_IsSortedByMostRecentActivity()
    {
        var fixture = new MessagingFixture();
        var second = ListingTestData.AddListing(fixture.Db, fixture.Listing.PublisherId).MoveTo(Imova.Domain.Listings.ListingStatus.Active);
        await fixture.Db.SaveChangesAsync();
        var older = await fixture.StartAsync();
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        var newer = await fixture.StartAsync(listingId: second.Id);
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        await fixture.SendAsync(fixture.Seller.Id, older!.ConversationId, "Răspuns");

        Assert.Equal(
            [older.ConversationId, newer!.ConversationId],
            (await fixture.InboxAsync(fixture.Seller.Id)).Select(c => c.Id));
    }

    [Fact]
    public async Task Search_MatchesTheOtherParticipantOrTheListing_IgnoringCaseAndDiacritics()
    {
        var fixture = new MessagingFixture();
        await fixture.StartAsync();

        Assert.Single(await fixture.InboxAsync(fixture.Seller.Id, "maria"));
        Assert.Single(await fixture.InboxAsync(fixture.Seller.Id, "APARTAMENT"));
        Assert.Empty(await fixture.InboxAsync(fixture.Seller.Id, "garaj"));
        Assert.Single(await fixture.InboxAsync(fixture.Visitor.Id, "ion popescu"));
    }

    [Fact]
    public async Task Archive_HidesItOnlyFromTheOneWhoArchived()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();
        var archive = new SetConversationArchivedHandler(fixture.Db);

        Assert.True(await archive.Handle(new SetConversationArchivedCommand(fixture.Seller.Id, started!.ConversationId, true), CancellationToken.None));

        Assert.Empty(await fixture.InboxAsync(fixture.Seller.Id));
        Assert.Single(await fixture.InboxAsync(fixture.Seller.Id, archived: true));
        Assert.Single(await fixture.InboxAsync(fixture.Visitor.Id));

        await archive.Handle(new SetConversationArchivedCommand(fixture.Seller.Id, started.ConversationId, false), CancellationToken.None);
        Assert.Single(await fixture.InboxAsync(fixture.Seller.Id));
    }

    [Fact]
    public async Task Thread_LoadsNewestPageFirst_ThenOlderOnes()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync(body: "Mesaj 0");
        for (var i = 1; i < 35; i++)
        {
            fixture.Clock.Advance(TimeSpan.FromSeconds(1));
            await fixture.SendAsync(i % 2 == 0 ? fixture.Visitor.Id : fixture.Seller.Id, started!.ConversationId, $"Mesaj {i}");
        }

        var latest = await fixture.ThreadAsync(fixture.Visitor.Id, started!.ConversationId, pageSize: 30);
        Assert.True(latest!.HasMore);
        Assert.Equal("Mesaj 5", latest.Messages[0].Body);
        Assert.Equal("Mesaj 34", latest.Messages[^1].Body);

        var older = await fixture.ThreadAsync(fixture.Visitor.Id, started.ConversationId, before: latest.Messages[0].Id, pageSize: 30);
        Assert.False(older!.HasMore);
        Assert.Equal(["Mesaj 0", "Mesaj 1", "Mesaj 2", "Mesaj 3", "Mesaj 4"], older.Messages.Select(m => m.Body));
    }

    [Fact]
    public async Task Thread_CarriesTheListingsKeyFacts_ForTheHeaderStrip()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();

        var listing = (await fixture.ThreadAsync(fixture.Visitor.Id, started!.ConversationId))!.Listing!;

        Assert.Equal(fixture.Listing.Id, listing.Id);
        Assert.Equal(fixture.Listing.Title, listing.Title);
        Assert.Equal("Apartment", listing.PropertyType);
        Assert.Equal("Rent", listing.TransactionType);
        Assert.Equal(2, listing.Rooms);
        Assert.Equal(54m, listing.TotalAreaM2);
        Assert.Equal((550m, "EUR"), (listing.PriceAmount, listing.PriceCurrency));
        Assert.True(listing.IsActive);
    }

    [Fact]
    public async Task Thread_ListingStrip_ReflectsAnInactiveOrDeletedListing()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();

        fixture.Listing.Archive();
        await fixture.Db.SaveChangesAsync();
        Assert.False((await fixture.ThreadAsync(fixture.Visitor.Id, started!.ConversationId))!.Listing!.IsActive);

        fixture.Db.Listings.Remove(fixture.Listing);
        await fixture.Db.SaveChangesAsync();
        Assert.Null((await fixture.ThreadAsync(fixture.Visitor.Id, started.ConversationId))!.Listing);
    }

    [Fact]
    public async Task Thread_ForAnOutsider_IsNotFound()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();

        Assert.Null(await fixture.ThreadAsync(Guid.NewGuid(), started!.ConversationId));
    }

    [Fact]
    public async Task ExistingConversationForAListing_IsFoundForTheVisitorOnly()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();
        var handler = new GetConversationIdForListingHandler(fixture.Db);

        Assert.Equal(started!.ConversationId, await handler.Handle(new(fixture.Visitor.Id, fixture.Listing.Id), CancellationToken.None));
        Assert.Null(await handler.Handle(new(fixture.Seller.Id, fixture.Listing.Id), CancellationToken.None));
    }
}
