using Imova.Application.Common.Identity;
using Imova.Application.Features.Messaging;
using Imova.Application.Features.Retention;
using Imova.Domain.Listings;
using Imova.Domain.Messaging;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Retention;

// The retention limits the privacy policy promises: old ended listings, inactive conversations and the
// IP on old admin audit entries.
public class DataRetentionCleanupTests
{
    private readonly MessagingFixture _f = new();

    private Task<int> RunAsync(DateTimeOffset now) =>
        new DataRetentionCleanup(_f.Db, _f.Blobs, new ManualTimeProvider(now), NullLogger<DataRetentionCleanup>.Instance)
            .RunAsync(CancellationToken.None);

    // Listing.UpdatedAt is the real clock (the domain sets it), so the job's "now" is placed around it.
    private static DateTimeOffset AfterEndedLimit(Listing listing, int days) => listing.UpdatedAt + Listing.EndedKeepFor + TimeSpan.FromDays(days);

    [Fact]
    public async Task AnEndedListing_IsDeletedWithItsPhotos_OnlyOnceItsRetentionPeriodHasPassed()
    {
        var sold = ListingTestData.AddListing(_f.Db, ListingTestData.AddIndividualPublisher(_f.Db, _f.Seller.Id).Id, TransactionType.Sale)
            .MoveTo(ListingStatus.Sold);
        ListingTestData.AddPhotos(_f.Db, sold.Id, 3);
        await _f.Db.SaveChangesAsync();

        await RunAsync(AfterEndedLimit(sold, -1));
        Assert.True(await _f.Db.Listings.AnyAsync(l => l.Id == sold.Id));

        await RunAsync(AfterEndedLimit(sold, 1));

        Assert.False(await _f.Db.Listings.AnyAsync(l => l.Id == sold.Id));
        Assert.False(await _f.Db.Photos.AnyAsync(p => p.ListingId == sold.Id));
        Assert.NotEmpty(_f.Blobs.DeletedBlobNames.Where(b => b.StartsWith($"{sold.Id}/")));
    }

    [Theory]
    [InlineData(ListingStatus.Active)]
    [InlineData(ListingStatus.Draft)]
    [InlineData(ListingStatus.Suspended)]
    public async Task AListingThatHasNotEnded_IsKept_HoweverOld(ListingStatus status)
    {
        var listing = ListingTestData.AddListing(_f.Db, ListingTestData.AddIndividualPublisher(_f.Db, _f.Seller.Id).Id).MoveTo(status);
        await _f.Db.SaveChangesAsync();

        await RunAsync(AfterEndedLimit(listing, 400));

        Assert.True(await _f.Db.Listings.AnyAsync(l => l.Id == listing.Id));
    }

    [Fact]
    public async Task AnInactiveConversation_IsDeletedWithItsMessagesAndImages_AfterTheLimit()
    {
        var image = _f.UploadedImage(_f.Visitor.Id);
        var started = (await _f.StartAsync(attachments: [image]))!;
        var start = _f.Clock.GetUtcNow();

        await RunAsync(start + Conversation.InactiveKeepFor - TimeSpan.FromDays(1));
        Assert.True(await _f.Db.Conversations.AnyAsync(c => c.Id == started.ConversationId));

        await RunAsync(start + Conversation.InactiveKeepFor + TimeSpan.FromDays(1));

        Assert.False(await _f.Db.Conversations.AnyAsync(c => c.Id == started.ConversationId));
        Assert.False(await _f.Db.Messages.AnyAsync(m => m.ConversationId == started.ConversationId));
        Assert.All(MessageImageSizes.AllBlobNames(image), name => Assert.Contains(name, _f.Blobs.DeletedMessageAttachmentNames));
    }

    [Fact]
    public async Task AConversationWithANewerMessage_IsKept()
    {
        var started = (await _f.StartAsync())!;
        _f.Clock.Advance(TimeSpan.FromDays(400));
        await _f.SendAsync(_f.Seller.Id, started.ConversationId);
        var start = _f.Clock.GetUtcNow() - TimeSpan.FromDays(400);

        await RunAsync(start + Conversation.InactiveKeepFor + TimeSpan.FromDays(1));

        Assert.True(await _f.Db.Conversations.AnyAsync(c => c.Id == started.ConversationId));
    }

    [Fact]
    public async Task AConversationWithAnOpenReport_WaitsForAnAdmin_ThenGoesOnceResolved()
    {
        var started = (await _f.StartAsync())!;
        var conversation = await _f.Db.Conversations.SingleAsync(c => c.Id == started.ConversationId);
        var report = ConversationReport.Create(conversation, _f.Seller.Id, ReportReason.Spam, null, _f.Clock.GetUtcNow());
        _f.Db.ConversationReports.Add(report);
        await _f.Db.SaveChangesAsync();
        var later = _f.Clock.GetUtcNow() + Conversation.InactiveKeepFor + TimeSpan.FromDays(1);

        await RunAsync(later);
        Assert.True(await _f.Db.Conversations.AnyAsync(c => c.Id == started.ConversationId));

        report.Resolve(Guid.NewGuid(), later);
        await _f.Db.SaveChangesAsync();
        await RunAsync(later);

        Assert.False(await _f.Db.Conversations.AnyAsync(c => c.Id == started.ConversationId));
        Assert.False(await _f.Db.ConversationReports.AnyAsync(r => r.ConversationId == started.ConversationId));
    }

    [Fact]
    public async Task AnOldAuditEntry_LosesItsIpAddress_ButIsKept()
    {
        var now = new DateTimeOffset(2027, 12, 1, 0, 0, 0, TimeSpan.Zero);
        var old = NewAuditEntry(now - AdminAuditEntry.IpKeepFor - TimeSpan.FromDays(1));
        var recent = NewAuditEntry(now - AdminAuditEntry.IpKeepFor + TimeSpan.FromDays(1));
        _f.Db.AdminAuditEntries.AddRange(old, recent);
        await _f.Db.SaveChangesAsync();

        await RunAsync(now);

        Assert.Null((await _f.Db.AdminAuditEntries.SingleAsync(e => e.Id == old.Id)).IpAddress);
        Assert.Equal("203.0.113.7", (await _f.Db.AdminAuditEntries.SingleAsync(e => e.Id == recent.Id)).IpAddress);
    }

    private static AdminAuditEntry NewAuditEntry(DateTimeOffset at) => new()
    {
        Id = Guid.NewGuid(),
        Action = AdminAuditEntry.GrantAdmin,
        ActorUserId = Guid.NewGuid(),
        TargetUserId = Guid.NewGuid(),
        CreatedAt = at,
        IpAddress = "203.0.113.7",
    };
}
