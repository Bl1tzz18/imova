using Imova.Domain.Agencies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Agencies;

public class AgencyPhoneRevealConfiguration : IEntityTypeConfiguration<AgencyPhoneReveal>
{
    public void Configure(EntityTypeBuilder<AgencyPhoneReveal> builder)
    {
        // The key is the once-a-day-per-visitor rule (INSERT … ON CONFLICT DO NOTHING).
        builder.HasKey(r => new { r.AgencyId, r.Day, r.VisitorHash });
        builder.Property(r => r.VisitorHash).HasMaxLength(64);
        builder.HasOne<Agency>().WithMany().HasForeignKey(r => r.AgencyId).OnDelete(DeleteBehavior.Cascade);

        // The cleanup job deletes by age.
        builder.HasIndex(r => r.RevealedAt);
    }
}
