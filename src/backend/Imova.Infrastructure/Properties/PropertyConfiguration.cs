using Imova.Domain.Amenities;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.Domain.Proximities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Properties;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PropertyType).IsRequired();
        // Wide enough for large land plots, which are also stored in m².
        builder.Property(p => p.TotalAreaM2).HasColumnType("numeric(12,2)");
        builder.Property(p => p.Condition);

        builder.Property(p => p.LocationId).IsRequired();
        builder.HasOne<PropertyLocation>().WithOne().HasForeignKey<Property>(p => p.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.LocationId).IsUnique();

        builder.Property(p => p.TypeSpecificAttributes)
            .HasColumnType("jsonb")
            .HasConversion(new PropertyAttributesConverter())
            .IsRequired();

        builder.HasMany(p => p.Amenities).WithOne().HasForeignKey(a => a.PropertyId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Amenities).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.Proximities).WithOne().HasForeignKey(p => p.PropertyId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Proximities).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => p.PropertyType);
    }
}

public class PropertyAmenityConfiguration : IEntityTypeConfiguration<PropertyAmenity>
{
    public void Configure(EntityTypeBuilder<PropertyAmenity> builder)
    {
        builder.ToTable("PropertyAmenities");
        builder.HasKey(a => new { a.PropertyId, a.AmenityId });
        builder.HasOne<Amenity>().WithMany().HasForeignKey(a => a.AmenityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => a.AmenityId);
    }
}

public class PropertyProximityConfiguration : IEntityTypeConfiguration<PropertyProximity>
{
    public void Configure(EntityTypeBuilder<PropertyProximity> builder)
    {
        builder.ToTable("PropertyProximities");
        builder.HasKey(p => new { p.PropertyId, p.ProximityId });
        builder.HasOne<Proximity>().WithMany().HasForeignKey(p => p.ProximityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.ProximityId);
    }
}
