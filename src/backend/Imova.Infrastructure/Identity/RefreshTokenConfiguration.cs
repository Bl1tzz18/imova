using Imova.Application.Common.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Identity;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(t => t.StampFingerprint).HasMaxLength(64).IsRequired();

        // Looked up by hash on every refresh.
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.SessionId);
        // The cleanup job.
        builder.HasIndex(t => t.ExpiresAt);

        // A deleted account takes its sessions with it.
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
