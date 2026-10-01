using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Listings;

// Same column types as the listing's own price (ListingConfiguration).
public class ListingPriceChangeConfiguration : IEntityTypeConfiguration<ListingPriceChange>
{
    public void Configure(EntityTypeBuilder<ListingPriceChange> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.OldAmount).HasColumnType("numeric(14,2)");
        builder.Property(c => c.OldCurrency).HasConversion<string>().HasMaxLength(3);
        builder.Property(c => c.OldPriceEur).HasColumnType("numeric(14,2)");
        builder.Property(c => c.NewAmount).HasColumnType("numeric(14,2)");
        builder.Property(c => c.NewCurrency).HasConversion<string>().HasMaxLength(3);
        builder.Property(c => c.NewPriceEur).HasColumnType("numeric(14,2)");

        // Deleting a listing takes its price history with it (ListingRemoval also removes the rows
        // explicitly, for the unit tests' in-memory database).
        builder.HasOne<Listing>().WithMany().HasForeignKey(c => c.ListingId).OnDelete(DeleteBehavior.Cascade);

        // A listing's history in order.
        builder.HasIndex(c => new { c.ListingId, c.ChangedAt });
    }
}
