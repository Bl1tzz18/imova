using Imova.Application.Common.Identity;
using Imova.Application.Features.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Telegram;

public class TelegramLinkConfiguration : IEntityTypeConfiguration<TelegramLink>
{
    public void Configure(EntityTypeBuilder<TelegramLink> builder)
    {
        builder.ToTable("TelegramLinks");
        // One chat per account…
        builder.HasKey(l => l.UserId);
        builder.Property(l => l.UserId).ValueGeneratedNever();
        // …and one account per chat; the bot looks chats up by id.
        builder.HasIndex(l => l.ChatId).IsUnique();
        builder.Property(l => l.TelegramUsername).HasMaxLength(64);
        builder.Property(l => l.TelegramFirstName).HasMaxLength(128);

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TelegramLinkCodeConfiguration : IEntityTypeConfiguration<TelegramLinkCode>
{
    public void Configure(EntityTypeBuilder<TelegramLinkCode> builder)
    {
        builder.ToTable("TelegramLinkCodes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CodeHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(c => c.CodeHash).IsUnique();
        builder.HasIndex(c => c.UserId);

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
