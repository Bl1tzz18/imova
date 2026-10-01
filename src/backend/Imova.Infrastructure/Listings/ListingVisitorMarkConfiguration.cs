using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Listings;

public class ListingVisitorMarkConfiguration : IEntityTypeConfiguration<ListingVisitorMark>
{
    public void Configure(EntityTypeBuilder<ListingVisitorMark> builder)
    {
        builder.HasKey(m => new { m.ListingId, m.Counter, m.VisitorHash });
        builder.Property(m => m.VisitorHash).HasMaxLength(64);
        builder.HasOne<Listing>().WithMany().HasForeignKey(m => m.ListingId).OnDelete(DeleteBehavior.Cascade);

        // The cleanup job deletes by age.
        builder.HasIndex(m => m.CountedAt);
    }
}
