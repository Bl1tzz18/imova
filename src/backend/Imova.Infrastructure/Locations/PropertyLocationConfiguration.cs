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

        builder.Property(l => l.RaionId).IsRequired();
        builder.Property(l => l.RaionName).IsRequired().HasMaxLength(100);
        builder.HasOne<Raion>().WithMany().HasForeignKey(l => l.RaionId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.LocalitateId);
        // Some flattened Chișinău entries are longer compound names than a typical locality.
        builder.Property(l => l.LocalitateName).HasMaxLength(150);
        builder.HasOne<Localitate>().WithMany().HasForeignKey(l => l.LocalitateId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.Sector).HasMaxLength(100);
        builder.Property(l => l.Street).HasMaxLength(200);
        builder.Property(l => l.BuildingNumber).HasMaxLength(20);

        builder.Property(l => l.Location).HasColumnType("geometry(Point,4326)");
        builder.HasIndex(l => l.Location).HasMethod("GIST");
    }
}
