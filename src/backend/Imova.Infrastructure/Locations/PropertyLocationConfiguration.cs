using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Locations;

public class PropertyLocationConfiguration : IEntityTypeConfiguration<PropertyLocation>
{
    public void Configure(EntityTypeBuilder<PropertyLocation> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.PropertyId).IsRequired();
        builder.HasOne<Property>().WithMany().HasForeignKey(l => l.PropertyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(l => l.PropertyId).IsUnique();

        builder.Property(l => l.Country).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Region).HasMaxLength(100);
        builder.Property(l => l.City).IsRequired().HasMaxLength(100);
        builder.Property(l => l.District).HasMaxLength(100);
        builder.Property(l => l.Sector).HasMaxLength(100);
        builder.Property(l => l.Street).HasMaxLength(200);
        builder.Property(l => l.BuildingNumber).HasMaxLength(20);

        builder.Property(l => l.Location).HasColumnType("geometry(Point,4326)");
        builder.HasIndex(l => l.Location).HasMethod("GIST");
    }
}
