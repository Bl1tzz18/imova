using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;

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

        builder.HasData(
            new
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444441"),
                PropertyId = SeedData.Property1Id,
                Country = "Moldova",
                Region = (string?)null,
                City = "Chisinau",
                District = "Botanica",
                Sector = (string?)null,
                Street = (string?)null,
                BuildingNumber = (string?)null,
                Latitude = 47.0021,
                Longitude = 28.8681,
                Location = new Point(28.8681, 47.0021) { SRID = 4326 }
            },
            new
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444442"),
                PropertyId = SeedData.Property2Id,
                Country = "Moldova",
                Region = (string?)null,
                City = "Chisinau",
                District = "Durlesti",
                Sector = (string?)null,
                Street = (string?)null,
                BuildingNumber = (string?)null,
                Latitude = 47.0575,
                Longitude = 28.7397,
                Location = new Point(28.7397, 47.0575) { SRID = 4326 }
            });
    }
}
