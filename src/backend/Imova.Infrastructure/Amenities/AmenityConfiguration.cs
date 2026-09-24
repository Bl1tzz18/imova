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
        new(ParkingId, "parking", "Parcare", AmenityCategory.Leisure),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000002"), "balcony", "Balcon/Logie", AmenityCategory.Comfort),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000003"), "elevator", "Ascensor", AmenityCategory.General),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000004"), "air_conditioning", "Aer condiționat", AmenityCategory.Comfort),
        new(FurnishedId, Amenity.FurnishedKey, "Mobilat", AmenityCategory.Comfort),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000006"), "garage", "Garaj", AmenityCategory.Leisure),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000007"), "yard", "Curte", AmenityCategory.Leisure),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000008"), "autonomous_heating", "Încălzire autonomă", AmenityCategory.General),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000009"), "centralized_heating", "Încălzire centralizată", AmenityCategory.General),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000a"), "wheelchair_access", "Acces pentru scaun cu rotile", AmenityCategory.Security),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000b"), "storage_room", "Debara", AmenityCategory.Leisure),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000c"), "video_surveillance", "Supraveghere video", AmenityCategory.Security),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000d"), "internet", "Internet", AmenityCategory.Comfort),

        // Comfort & interior
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000e"), "fireplace", "Șemineu", AmenityCategory.Comfort),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000f"), "underfloor_heating", "Încălzire în pardoseală", AmenityCategory.Comfort),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000010"), "smart_home", "Sistem casă inteligentă", AmenityCategory.Comfort),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000011"), "appliances", "Cu tehnică de uz casnic", AmenityCategory.Comfort),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000012"), "cable_tv", "Televiziune prin cablu", AmenityCategory.Comfort),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000013"), "landline", "Telefon fix", AmenityCategory.Comfort),

        // Security & access
        new(Guid.Parse("a1000000-0000-0000-0000-000000000014"), "intercom", "Interfon", AmenityCategory.Security),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000015"), "alarm_system", "Sistem de alarmă", AmenityCategory.Security),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000016"), "remote_gate", "Poartă cu telecomandă", AmenityCategory.Security),

        // Leisure & auxiliary spaces
        new(Guid.Parse("a1000000-0000-0000-0000-000000000017"), "sauna", "Saună", AmenityCategory.Leisure),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000018"), "basement", "Beci/subsol", AmenityCategory.Leisure),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000019"), "gazebo", "Foișor", AmenityCategory.Leisure),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000001a"), "pool", "Piscină", AmenityCategory.Leisure),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000001b"), "terrace", "Terasă", AmenityCategory.Leisure),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000001c"), "garden", "Grădină/seră", AmenityCategory.Leisure),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000001d"), "staff_room", "Cameră pentru personal/pază", AmenityCategory.Leisure),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000001e"), "backup_generator", "Generator de rezervă", AmenityCategory.Leisure),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000001f"), "water_purification", "Sistem de purificare a apei", AmenityCategory.Leisure),
    ];

    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Key).IsRequired().HasMaxLength(50);
        builder.HasIndex(a => a.Key).IsUnique();
        builder.Property(a => a.LabelRo).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Category).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.HasData(Seed);
    }
}
