using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Messaging.Admin;
using Imova.Application.Features.Messaging.SendMessage;
using Imova.Application.Features.Messaging.SetUserBlocked;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Messaging;

public class SendMessageTests
{
    private static async Task<(MessagingFixture Fixture, Guid ConversationId)> StartedAsync()
    {
        var fixture = new MessagingFixture();
        var started = await fixture.StartAsync();
        return (fixture, started!.ConversationId);
    }

    [Fact]
    public async Task Reply_IsPushedLiveToBothParticipants_WithTheRecipientsUnreadCount()
    {
        var (fixture, conversationId) = await StartedAsync();

        var reply = await fixture.SendAsync(fixture.Seller.Id, conversationId, "Da, este disponibil.");

        var pushed = fixture.Realtime.Created.Last();
        Assert.Equal(fixture.Visitor.Id, pushed.RecipientUserId);
        Assert.Equal(fixture.Seller.Id, pushed.SenderUserId);
        Assert.Equal(reply!.Id, pushed.Message.Id);
        Assert.Equal("Sent", reply.Status);
        Assert.Equal((fixture.Visitor.Id, 1), fixture.Realtime.UnreadCounts.Last());
    }

    [Fact]
    public async Task BlockedUser_CantSendInAnyConversationWithTheBlocker()
    {
        var (fixture, conversationId) = await StartedAsync();
        var blocked = await new SetUserBlockedHandler(fixture.Db, fixture.Clock)
            .Handle(new SetUserBlockedCommand(fixture.Seller.Id, conversationId, true), CancellationToken.None);
        Assert.True(blocked);

        var error = await Assert.ThrowsAsync<ForbiddenAccessException>(() => fixture.SendAsync(fixture.Visitor.Id, conversationId));
        Assert.Contains("blocked", error.Message);

        // …including a conversation about another of the blocker's listings.
        var other = ListingTestData.AddListing(fixture.Db, fixture.Listing.PublisherId).MoveTo(Imova.Domain.Listings.ListingStatus.Active);
        await fixture.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => fixture.StartAsync(listingId: other.Id));

        // The blocker can't write either while the block stands…
        var blockerError = await Assert.ThrowsAsync<ForbiddenAccessException>(() => fixture.SendAsync(fixture.Seller.Id, conversationId));
        Assert.Contains("unblock", blockerError.Message);

        // …and unblocking lets both sides write again.
        await new SetUserBlockedHandler(fixture.Db, fixture.Clock)
            .Handle(new SetUserBlockedCommand(fixture.Seller.Id, conversationId, false), CancellationToken.None);
        Assert.NotNull(await fixture.SendAsync(fixture.Visitor.Id, conversationId));
        Assert.NotNull(await fixture.SendAsync(fixture.Seller.Id, conversationId));
    }

    [Fact]
    public async Task BlockedState_IsShownInTheThreadToBothSides()
    {
        var (fixture, conversationId) = await StartedAsync();
        await new SetUserBlockedHandler(fixture.Db, fixture.Clock)
            .Handle(new SetUserBlockedCommand(fixture.Seller.Id, conversationId, true), CancellationToken.None);

        Assert.True((await fixture.ThreadAsync(fixture.Seller.Id, conversationId))!.BlockedByMe);
        Assert.True((await fixture.ThreadAsync(fixture.Visitor.Id, conversationId))!.BlockedByOther);
    }

    [Fact]
    public async Task UserBannedFromMessaging_CantSend()
    {
        var (fixture, conversationId) = await StartedAsync();
        await new SetMessagingBanHandler(fixture.Db)
            .Handle(new SetMessagingBanCommand(IsAdmin: true, fixture.Visitor.Id, Banned: true), CancellationToken.None);

        var error = await Assert.ThrowsAsync<ForbiddenAccessException>(() => fixture.SendAsync(fixture.Visitor.Id, conversationId));
        Assert.Contains("banned", error.Message);
    }

    [Fact]
    public async Task OnlyParticipants_CanSend()
    {
        var (fixture, conversationId) = await StartedAsync();
        var stranger = fixture.AddUser("Străin", "strain@example.com");

        Assert.Null(await fixture.SendAsync(stranger.Id, conversationId));
    }

    [Fact]
    public async Task SuspiciousContent_IsDeliveredButFlaggedForAdmins()
    {
        var (fixture, conversationId) = await StartedAsync();

        var message = await fixture.SendAsync(fixture.Seller.Id, conversationId, "Pentru rezervare plătește în avans prin Western Union");

        Assert.NotNull(message);
        var stored = await fixture.Db.Messages.SingleAsync(m => m.Id == message!.Id);
        Assert.True(stored.IsFlagged);
        Assert.Equal("Money transfer service", stored.FlagReason);
        var flagged = await new GetFlaggedMessagesHandler(fixture.Db)
            .Handle(new GetFlaggedMessagesQuery(IsAdmin: true), CancellationToken.None);
        Assert.Equal(message!.Id, Assert.Single(flagged).Message.Id);
        Assert.Equal("Ion Popescu", flagged[0].Sender.DisplayName);
    }

    [Fact]
    public async Task Attachments_OwnUploadedImagesAreAccepted()
    {
        var (fixture, conversationId) = await StartedAsync();
        var images = new[] { fixture.UploadedImage(fixture.Seller.Id), fixture.UploadedImage(fixture.Seller.Id) };

        var message = await fixture.SendAsync(fixture.Seller.Id, conversationId, "", images);

        Assert.Equal(2, message!.Attachments.Count);
        Assert.All(message.Attachments, a => Assert.Equal("image/png", a.ContentType));
        // An access-checked API path — never the storage URL or blob name.
        Assert.Equal($"/api/v1/messaging/attachments/{message.Attachments[0].Id}", message.Attachments[0].Url);
        Assert.All(message.Attachments, a => Assert.DoesNotContain("messages/", a.Url));
    }

    [Fact]
    public async Task Attachments_SomeoneElsesUploadOrAMissingFile_AreRejected()
    {
        var (fixture, conversationId) = await StartedAsync();

        await Assert.ThrowsAsync<ValidationException>(() =>
            fixture.SendAsync(fixture.Seller.Id, conversationId, "x", [fixture.UploadedImage(fixture.Visitor.Id)]));
        await Assert.ThrowsAsync<ValidationException>(() =>
            fixture.SendAsync(fixture.Seller.Id, conversationId, "x", [$"messages/{fixture.Seller.Id}/never-uploaded.png"]));
    }

    [Fact]
    public async Task Attachments_MustBeInThePrivateContainer_NotThePublicOne()
    {
        var (fixture, conversationId) = await StartedAsync();
        var publicBlob = $"messages/{fixture.Seller.Id}/public.png";
        fixture.Blobs.BlobInfoByName[publicBlob] = new Imova.Application.Common.Interfaces.UploadedBlobInfo(
            1024, "image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        await Assert.ThrowsAsync<ValidationException>(() => fixture.SendAsync(fixture.Seller.Id, conversationId, "x", [publicBlob]));
    }

    [Fact]
    public void Validator_RequiresTextOrAnImage_AndCapsBoth()
    {
        var validator = new SendMessageValidator();
        var id = Guid.NewGuid();

        Assert.False(validator.Validate(new SendMessageCommand(id, id, "  ", null)).IsValid);
        Assert.False(validator.Validate(new SendMessageCommand(id, id, new string('a', 2001), null)).IsValid);
        Assert.False(validator.Validate(new SendMessageCommand(id, id, "x", ["1", "2", "3", "4", "5", "6"])).IsValid);
        Assert.True(validator.Validate(new SendMessageCommand(id, id, null, ["1"])).IsValid);
        Assert.True(validator.Validate(new SendMessageCommand(id, id, new string('a', 2000), null)).IsValid);
    }
}
