using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Listings;

public class ListingDailyStatConfiguration : IEntityTypeConfiguration<ListingDailyStat>
{
    public void Configure(EntityTypeBuilder<ListingDailyStat> builder)
    {
        // One row per listing and day; ListingCounters upserts into it.
        builder.HasKey(s => new { s.ListingId, s.Day });
        builder.HasOne<Listing>().WithMany().HasForeignKey(s => s.ListingId).OnDelete(DeleteBehavior.Cascade);

        // The statistics read a date range across an agency's listings.
        builder.HasIndex(s => s.Day);
    }
}
