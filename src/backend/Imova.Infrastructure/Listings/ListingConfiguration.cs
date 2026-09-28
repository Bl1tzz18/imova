using System.Text.Json;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Imova.Domain.Publishers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Listings;

public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    private static readonly JsonSerializerOptions DetailsJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.PropertyId).IsRequired();
        builder.HasOne<Property>().WithMany().HasForeignKey(l => l.PropertyId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.PublisherId).IsRequired();
        builder.HasOne<Publisher>().WithMany().HasForeignKey(l => l.PublisherId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.TransactionType).IsRequired();
        builder.Property(l => l.Status).IsRequired();
        builder.Property(l => l.Title).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Description).IsRequired().HasMaxLength(4000);
        builder.Property(l => l.RejectionReason).HasMaxLength(1000);
        builder.Property(l => l.SuspensionReason).HasMaxLength(1000);

        // An owned type (not a complex property) mapped onto Listing's own columns — same schema
        // either way, but the EF InMemory provider the unit tests use can't query complex types.
        builder.OwnsOne(l => l.Price, price =>
        {
            price.Property(p => p.Amount).HasColumnName("PriceAmount").HasColumnType("numeric(14,2)");
            price.Property(p => p.Currency).HasColumnName("PriceCurrency").HasConversion<string>().HasMaxLength(3);
            price.Property(p => p.PriceEur).HasColumnName("PriceEur").HasColumnType("numeric(14,2)");
            price.Property(p => p.IsNegotiable).HasColumnName("PriceIsNegotiable");
            // Search: price range filters and price sorting.
            price.HasIndex(p => p.PriceEur);
        });
        builder.Navigation(l => l.Price).IsRequired();

        // Offer terms that only exist for one TransactionType — JSONB rather than a set of
        // mostly-null columns.
        builder.Property(l => l.SaleDetails)
            .HasColumnType("jsonb")
            .HasConversion(
                details => details == null ? null : JsonSerializer.Serialize(details, DetailsJsonOptions),
                json => json == null ? null : JsonSerializer.Deserialize<SaleDetails>(json, DetailsJsonOptions));
        builder.Property(l => l.RentalDetails)
            .HasColumnType("jsonb")
            .HasConversion(
                details => details == null ? null : JsonSerializer.Serialize(details, DetailsJsonOptions),
                json => json == null ? null : JsonSerializer.Deserialize<RentalDetails>(json, DetailsJsonOptions));
        // Nullable only for listings created before the Contact step existed.
        builder.Property(l => l.Contact)
            .HasColumnType("jsonb")
            .HasConversion(
                contact => contact == null ? null : JsonSerializer.Serialize(contact, DetailsJsonOptions),
                json => json == null ? null : JsonSerializer.Deserialize<ListingContact>(json, DetailsJsonOptions));

        builder.HasIndex(l => l.PropertyId);
        builder.HasIndex(l => l.PublisherId);
        builder.HasIndex(l => l.Status);
        // Search: every query filters Active, most also by Sale/Rent.
        builder.HasIndex(l => new { l.Status, l.TransactionType });
    }
}
