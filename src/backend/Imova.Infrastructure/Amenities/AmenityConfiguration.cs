using Imova.Domain.Amenities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Amenities;

public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    // Fixed ids: seeded via HasData (so they land in a migration), and the Property→Listing data
    // migration references Parking/Furnished by id to carry over the old boolean columns.
    public static readonly Guid ParkingId = Guid.Parse("a1000000-0000-0000-0000-000000000001");
    public static readonly Guid FurnishedId = Guid.Parse("a1000000-0000-0000-0000-000000000005");

    public static readonly IReadOnlyList<Amenity> Seed =
    [
        new(ParkingId, "parking", "Parcare"),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000002"), "balcony", "Balcon/Logie"),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000003"), "elevator", "Ascensor"),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000004"), "air_conditioning", "Aer condiționat"),
        new(FurnishedId, "furnished", "Mobilat"),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000006"), "garage", "Garaj"),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000007"), "yard", "Curte"),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000008"), "autonomous_heating", "Încălzire autonomă"),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000009"), "centralized_heating", "Încălzire centralizată"),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000a"), "wheelchair_access", "Acces pentru scaun cu rotile"),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000b"), "storage_room", "Debara"),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000c"), "video_surveillance", "Supraveghere video"),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000d"), "internet", "Internet"),
    ];

    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Key).IsRequired().HasMaxLength(50);
        builder.HasIndex(a => a.Key).IsUnique();
        builder.Property(a => a.LabelRo).IsRequired().HasMaxLength(100);

        builder.HasData(Seed);
    }
}
