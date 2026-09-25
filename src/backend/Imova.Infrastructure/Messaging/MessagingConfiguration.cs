using Imova.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Messaging;

// Deliberately no FK from Conversation to Listing (same as Photo): deleting a listing must not
// take its conversations — and any reports about them — down with it.
public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(c => c.Id);
        // One conversation per visitor per listing — the reuse rule, enforced even under races.
        builder.HasIndex(c => new { c.ListingId, c.InitiatorUserId }).IsUnique();
        builder.HasIndex(c => new { c.InitiatorUserId, c.LastMessageAt });
        builder.HasIndex(c => new { c.PublisherUserId, c.LastMessageAt });
        builder.HasIndex(c => new { c.InitiatorUserId, c.CreatedAt });
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Body).IsRequired().HasMaxLength(Message.MaxBodyLength);
        builder.Property(m => m.FlagReason).HasMaxLength(100);
        builder.Ignore(m => m.Status);
        builder.HasOne<Conversation>().WithMany().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt });
        builder.HasIndex(m => m.IsFlagged).HasFilter("\"IsFlagged\"");

        builder.HasMany(m => m.Attachments).WithOne().HasForeignKey(a => a.MessageId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(m => m.Attachments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
{
    public void Configure(EntityTypeBuilder<MessageAttachment> builder)
    {
        builder.ToTable("MessageAttachments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.BlobName).IsRequired().HasMaxLength(300);
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(100);
    }
}

public class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        builder.HasKey(b => new { b.BlockerUserId, b.BlockedUserId });
        builder.HasIndex(b => b.BlockedUserId);
    }
}

public class ConversationReportConfiguration : IEntityTypeConfiguration<ConversationReport>
{
    public void Configure(EntityTypeBuilder<ConversationReport> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Reason).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Details).HasMaxLength(ConversationReport.MaxDetailsLength);
        builder.HasOne<Conversation>().WithMany().HasForeignKey(r => r.ConversationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(r => r.ResolvedAt);
    }
}
