using Imova.Application.Common.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Identity;

public class AdminAuditEntryConfiguration : IEntityTypeConfiguration<AdminAuditEntry>
{
    public void Configure(EntityTypeBuilder<AdminAuditEntry> builder)
    {
        builder.ToTable("AdminAuditEntries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Action).HasMaxLength(50).IsRequired();
        builder.Property(e => e.IpAddress).HasMaxLength(45);
        builder.HasIndex(e => e.TargetUserId);
        builder.HasIndex(e => e.CreatedAt);
    }
}
