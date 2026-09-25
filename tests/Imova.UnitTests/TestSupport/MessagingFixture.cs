using Imova.Application.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Messaging;
using Imova.Application.Features.Messaging.GetConversations;
using Imova.Application.Features.Messaging.GetConversationThread;
using Imova.Application.Features.Messaging.GetUnreadCount;
using Imova.Application.Features.Messaging.MarkConversationRead;
using Imova.Application.Features.Messaging.MarkMessagesDelivered;
using Imova.Application.Features.Messaging.SendMessage;
using Imova.Application.Features.Messaging.StartConversation;
using Imova.Contracts.Messaging;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.TestSupport;

// A seller (Ion) with an Active listing, and a visitor (Maria) — plus every fake the messaging
// handlers need, and one-liners to run them.
internal sealed class MessagingFixture
{
    public ImovaDbContext Db { get; } = TestDbContextFactory.Create();

    public FakeBlobStorageService Blobs { get; } = new();

    public FakeRealtimeNotifier Realtime { get; } = new();

    public FakePresenceTracker Presence { get; } = new();

    public FakeEmailSender Email { get; } = new();

    public ManualTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 9, 25, 10, 0, 0, TimeSpan.Zero));

    public MessagingOptions Options { get; } = new();

    public ApplicationUser Seller { get; }

    public ApplicationUser Visitor { get; }

    public Listing Listing { get; }

    public MessagingFixture(ListingContact? contact = null)
    {
        Seller = AddUser("Ion Popescu", "ion@example.com");
        Visitor = AddUser("Maria Rusu", "maria@example.com");
        var publisher = ListingTestData.AddIndividualPublisher(Db, Seller.Id);
        Listing = ListingTestData.AddListing(Db, publisher.Id, contact: contact).MoveTo(ListingStatus.Active);
        Db.SaveChanges();
    }

    public ApplicationUser AddUser(string name, string email)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), DisplayName = name, Email = email, UserName = email };
        Db.Users.Add(user);
        Db.SaveChanges();
        return user;
    }

    public MessageDelivery Delivery() =>
        new(Db, Blobs, Realtime, Presence, Email, Options, Clock, NullLogger<MessageDelivery>.Instance);

    public Task<StartConversationResultDto?> StartAsync(Guid? userId = null, Guid? listingId = null, string body = "Bună ziua!", IReadOnlyList<string>? attachments = null) =>
        new StartConversationHandler(Db, Delivery(), Options, Clock)
            .Handle(new StartConversationCommand(userId ?? Visitor.Id, listingId ?? Listing.Id, body, attachments), CancellationToken.None);

    public Task<MessageDto?> SendAsync(Guid userId, Guid conversationId, string body = "Mai este disponibil?", IReadOnlyList<string>? attachments = null) =>
        new SendMessageHandler(Db, Delivery())
            .Handle(new SendMessageCommand(userId, conversationId, body, attachments), CancellationToken.None);

    public Task<bool> MarkReadAsync(Guid userId, Guid conversationId) =>
        new MarkConversationReadHandler(Db, Realtime, Clock, NullLogger<MarkConversationReadHandler>.Instance)
            .Handle(new MarkConversationReadCommand(userId, conversationId), CancellationToken.None);

    public Task<int> MarkDeliveredAsync(Guid userId) =>
        new MarkMessagesDeliveredHandler(Db, Realtime, Clock, NullLogger<MarkMessagesDeliveredHandler>.Instance)
            .Handle(new MarkMessagesDeliveredCommand(userId), CancellationToken.None);

    public async Task<int> UnreadAsync(Guid userId) =>
        (await new GetUnreadCountHandler(Db).Handle(new GetUnreadCountQuery(userId), CancellationToken.None)).Count;

    public Task<List<ConversationSummaryDto>> InboxAsync(Guid userId, string? search = null, bool archived = false) =>
        new GetConversationsHandler(Db, Blobs).Handle(new GetConversationsQuery(userId, search, archived), CancellationToken.None);

    public Task<ConversationThreadDto?> ThreadAsync(Guid userId, Guid conversationId, Guid? before = null, int pageSize = 30) =>
        new GetConversationThreadHandler(Db, Blobs)
            .Handle(new GetConversationThreadQuery(userId, conversationId, before, pageSize), CancellationToken.None);

    // A real PNG header, so ImageSignature accepts the "uploaded" blob.
    public string UploadedImage(Guid ownerUserId)
    {
        var name = Blobs.GenerateMessageAttachmentBlobName(ownerUserId, ".png");
        Blobs.MessageAttachmentInfoByName[name] = new UploadedBlobInfo(1024, "image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        return name;
    }
}
