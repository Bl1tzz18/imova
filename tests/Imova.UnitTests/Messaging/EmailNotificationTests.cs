using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Messaging;

public class EmailNotificationTests
{
    [Fact]
    public async Task FirstMessage_EmailsThePublisher_WithALinkToTheConversation()
    {
        var fixture = new MessagingFixture();

        var started = await fixture.StartAsync(body: "Bună ziua, mai este disponibil?");

        var email = Assert.Single(fixture.Email.Sent);
        Assert.Equal("ion@example.com", email.To);
        Assert.Contains("Maria Rusu", email.TextBody);
        Assert.Contains("mai este disponibil", email.TextBody);
        Assert.Contains($"http://localhost:3000/messages/{started!.ConversationId}", email.TextBody);
    }

    [Fact]
    public async Task ABurstOfMessages_SendsOneEmail()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();

        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        await fixture.SendAsync(fixture.Visitor.Id, started!.ConversationId);
        fixture.Clock.Advance(TimeSpan.FromMinutes(30));
        // Still unread — no email, however long it's been.
        await fixture.SendAsync(fixture.Visitor.Id, started.ConversationId);

        Assert.Single(fixture.Email.Sent);
    }

    [Fact]
    public async Task AfterReading_TheNextMessageEmailsOnlyOnceTheTenMinuteCooldownHasPassed()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();
        var conversationId = started!.ConversationId;

        await fixture.MarkReadAsync(fixture.Seller.Id, conversationId);
        fixture.Clock.Advance(TimeSpan.FromMinutes(5));
        await fixture.SendAsync(fixture.Visitor.Id, conversationId);
        Assert.Single(fixture.Email.Sent);

        await fixture.MarkReadAsync(fixture.Seller.Id, conversationId);
        fixture.Clock.Advance(TimeSpan.FromMinutes(10));
        await fixture.SendAsync(fixture.Visitor.Id, conversationId);
        Assert.Equal(2, fixture.Email.Sent.Count);
    }

    [Fact]
    public async Task RecipientOnTheSite_IsNotEmailed()
    {
        var fixture = new MessagingFixture();
        fixture.Presence.Online.Add(fixture.Seller.Id);

        await fixture.StartAsync();

        Assert.Empty(fixture.Email.Sent);
        Assert.Single(fixture.Realtime.Created);
    }

    [Fact]
    public async Task TheCooldownIsPerRecipient()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();

        await fixture.SendAsync(fixture.Seller.Id, started!.ConversationId, "Da, este.");

        Assert.Equal(["ion@example.com", "maria@example.com"], fixture.Email.Sent.Select(e => e.To));
    }
}
