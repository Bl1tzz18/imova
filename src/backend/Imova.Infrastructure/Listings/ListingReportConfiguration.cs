using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Listings;

public class ListingReportConfiguration : IEntityTypeConfiguration<ListingReport>
{
    public void Configure(EntityTypeBuilder<ListingReport> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Reason).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Outcome).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Details).HasMaxLength(ListingReport.MaxDetailsLength);
        builder.Property(r => r.ResolutionNote).HasMaxLength(ListingReport.MaxNoteLength);

        // Deleting a listing takes its reports with it (ListingRemoval also removes them explicitly,
        // for the unit tests' in-memory database). The reporter has no FK — see ListingReport.
        builder.HasOne<Listing>().WithMany().HasForeignKey(r => r.ListingId).OnDelete(DeleteBehavior.Cascade);

        // One open report per reporter and listing (reporting again amends it) — holds even when
        // two submissions race.
        builder.HasIndex(r => new { r.ListingId, r.ReporterUserId })
            .IsUnique()
            .HasFilter("\"ResolvedAt\" IS NULL");

        // The admin queue (open reports) and the per-reporter daily limit.
        builder.HasIndex(r => r.ResolvedAt);
        builder.HasIndex(r => new { r.ReporterUserId, r.CreatedAt });
    }
}
