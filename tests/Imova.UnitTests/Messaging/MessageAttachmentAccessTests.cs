using Imova.Application.Features.Messaging.GetMessageAttachment;
using Imova.Application.Features.Messaging.RequestAttachmentUploadUrl;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Messaging;

public class MessageAttachmentAccessTests
{
    private static async Task<(MessagingFixture Fixture, Guid AttachmentId, string BlobName)> ImageSentAsync()
    {
        var fixture = new MessagingFixture();
        var blobName = fixture.UploadedImage(fixture.Visitor.Id);
        var started = await fixture.StartAsync(body: "Uitați o poză", attachments: [blobName]);
        return (fixture, started!.Message.Attachments.Single().Id, blobName);
    }

    private static Task<MessageAttachmentFile?> GetAsync(MessagingFixture fixture, Guid userId, Guid attachmentId, bool isAdmin = false) =>
        new GetMessageAttachmentHandler(fixture.Db)
            .Handle(new GetMessageAttachmentQuery(userId, isAdmin, attachmentId), CancellationToken.None);

    [Fact]
    public async Task BothParticipants_CanGetTheImage()
    {
        var (fixture, attachmentId, blobName) = await ImageSentAsync();

        var forSender = await GetAsync(fixture, fixture.Visitor.Id, attachmentId);
        var forRecipient = await GetAsync(fixture, fixture.Seller.Id, attachmentId);

        Assert.Equal(new MessageAttachmentFile(blobName, "image/png"), forSender);
        Assert.Equal(forSender, forRecipient);
    }

    [Fact]
    public async Task AnAdmin_CanGetTheImage()
    {
        var (fixture, attachmentId, _) = await ImageSentAsync();

        Assert.NotNull(await GetAsync(fixture, Guid.NewGuid(), attachmentId, isAdmin: true));
    }

    [Fact]
    public async Task AnyoneElse_GetsNothing_SameAsForAnUnknownId()
    {
        var (fixture, attachmentId, _) = await ImageSentAsync();
        var stranger = fixture.AddUser("Străin", "strain@example.com");

        Assert.Null(await GetAsync(fixture, stranger.Id, attachmentId));
        Assert.Null(await GetAsync(fixture, fixture.Visitor.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task UploadUrl_PointsAtThePrivateContainer()
    {
        var fixture = new MessagingFixture();

        var target = await new RequestAttachmentUploadUrlHandler(fixture.Blobs, fixture.Clock)
            .Handle(new RequestAttachmentUploadUrlCommand(fixture.Visitor.Id, ".png"), CancellationToken.None);

        Assert.StartsWith($"messages/{fixture.Visitor.Id}/", target.BlobName);
        Assert.StartsWith("https://blob.test/private/", target.UploadUrl);
    }
}
