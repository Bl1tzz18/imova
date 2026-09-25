using Imova.Domain.Amenities;
using Imova.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Amenities;

public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    // Fixed ids: seeded via HasData (so they land in a migration), and the Property→Listing data
    // migration references Parking/Furnished by id to carry over the old boolean columns.
    public static readonly Guid ParkingId = Guid.Parse("a1000000-0000-0000-0000-000000000001");
    public static readonly Guid FurnishedId = Guid.Parse("a1000000-0000-0000-0000-000000000005");

    // ApplicablePropertyTypes decides where each amenity is offered (and accepted); Category
    // groups it into the details form's sections — General ones land in each layout's catch-all
    // section (see detailLayouts.ts on the frontend).
    public static readonly IReadOnlyList<Amenity> Seed =
    [
        new(ParkingId, "parking", "Parcare", AmenityCategory.Leisure, [PropertyType.Apartment, PropertyType.House, PropertyType.Commercial]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000002"), "balcony", "Balcon/Logie", AmenityCategory.General, [PropertyType.Apartment, PropertyType.House, PropertyType.Room]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000003"), "elevator", "Ascensor", AmenityCategory.General, [PropertyType.Apartment, PropertyType.Commercial]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000004"), "air_conditioning", "Aer condiționat", AmenityCategory.Comfort, [PropertyType.Apartment, PropertyType.House, PropertyType.Commercial, PropertyType.Room]),
        new(FurnishedId, Amenity.FurnishedKey, "Mobilat", AmenityCategory.Comfort, [PropertyType.Apartment, PropertyType.House, PropertyType.Commercial, PropertyType.Room]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000006"), "garage", "Garaj", AmenityCategory.Leisure, [PropertyType.Apartment, PropertyType.House]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000007"), "yard", "Curte", AmenityCategory.Leisure, [PropertyType.House]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000008"), "autonomous_heating", "Încălzire autonomă", AmenityCategory.General, [PropertyType.Commercial, PropertyType.Room]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000009"), "centralized_heating", "Încălzire centralizată", AmenityCategory.General, [PropertyType.Commercial, PropertyType.Room]),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000a"), "wheelchair_access", "Acces pentru scaun cu rotile", AmenityCategory.Security, [PropertyType.Apartment, PropertyType.House, PropertyType.Commercial]),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000b"), "storage_room", "Debara", AmenityCategory.Leisure, [PropertyType.Apartment, PropertyType.House, PropertyType.Commercial]),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000c"), "video_surveillance", "Supraveghere video", AmenityCategory.Security, [PropertyType.Apartment, PropertyType.House, PropertyType.Commercial, PropertyType.Garage]),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000d"), "internet", "Internet", AmenityCategory.Comfort, [PropertyType.Apartment, PropertyType.House, PropertyType.Commercial, PropertyType.Room]),

        // House
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000e"), "fireplace", "Șemineu", AmenityCategory.Comfort, [PropertyType.House]),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000000f"), "underfloor_heating", "Încălzire în pardoseală", AmenityCategory.Comfort, [PropertyType.Apartment, PropertyType.House]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000010"), "smart_home", "Sistem casă inteligentă", AmenityCategory.Comfort, [PropertyType.Apartment, PropertyType.House]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000011"), "appliances", "Cu tehnică de uz casnic", AmenityCategory.Comfort, [PropertyType.Apartment, PropertyType.House, PropertyType.Room]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000012"), "cable_tv", "Televiziune prin cablu", AmenityCategory.Comfort, [PropertyType.Apartment, PropertyType.House, PropertyType.Room]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000013"), "landline", "Telefon fix", AmenityCategory.Comfort, [PropertyType.Apartment, PropertyType.House]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000014"), "intercom", "Interfon", AmenityCategory.Security, [PropertyType.Apartment, PropertyType.House, PropertyType.Commercial]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000015"), "alarm_system", "Sistem de alarmă", AmenityCategory.Security, [PropertyType.Apartment, PropertyType.House, PropertyType.Commercial, PropertyType.Garage]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000016"), "remote_gate", "Poartă cu telecomandă", AmenityCategory.Security, [PropertyType.House, PropertyType.Garage]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000017"), "sauna", "Saună", AmenityCategory.Leisure, [PropertyType.House]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000018"), "basement", "Beci/subsol", AmenityCategory.Leisure, [PropertyType.House]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000019"), "gazebo", "Foișor", AmenityCategory.Leisure, [PropertyType.House]),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000001a"), "pool", "Piscină", AmenityCategory.Leisure, [PropertyType.House]),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000001b"), "terrace", "Terasă", AmenityCategory.Leisure, [PropertyType.House]),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000001c"), "garden", "Grădină/seră", AmenityCategory.Leisure, [PropertyType.House]),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000001d"), "staff_room", "Cameră pentru personal/pază", AmenityCategory.Leisure, [PropertyType.House]),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000001e"), "backup_generator", "Generator de rezervă", AmenityCategory.Leisure, [PropertyType.House, PropertyType.Commercial]),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000001f"), "water_purification", "Sistem de purificare a apei", AmenityCategory.Leisure, [PropertyType.House]),

        // Apartment
        new(Guid.Parse("a1000000-0000-0000-0000-000000000020"), "dishwasher", "Mașină de spălat vase", AmenityCategory.Comfort, [PropertyType.Apartment, PropertyType.House, PropertyType.Room]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000021"), "armored_door", "Ușă blindată", AmenityCategory.Security, [PropertyType.Apartment, PropertyType.House]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000022"), "storage_annex", "Anexă/boxă", AmenityCategory.General, [PropertyType.Apartment]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000023"), "separate_entrance", "Intrare separată", AmenityCategory.General, [PropertyType.Apartment, PropertyType.Commercial]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000024"), "panoramic_windows", "Geamuri panoramice", AmenityCategory.Comfort, [PropertyType.Apartment, PropertyType.House, PropertyType.Commercial]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000025"), "separate_living_room", "Living separat", AmenityCategory.Comfort, [PropertyType.Apartment]),
        new(Guid.Parse("a1000000-0000-0000-0000-000000000026"), "thermopane_windows", "Geamuri termopan", AmenityCategory.Comfort, [PropertyType.Apartment, PropertyType.Commercial, PropertyType.Room]),

        // Garage and Room
        new(Guid.Parse("a1000000-0000-0000-0000-00000000002a"), "electricity", "Electricitate", AmenityCategory.General, [PropertyType.Garage]),
        new(Guid.Parse("a1000000-0000-0000-0000-00000000002b"), "kitchen_access", "Acces la bucătărie", AmenityCategory.Comfort, [PropertyType.Room]),
    ];

    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Key).IsRequired().HasMaxLength(50);
        builder.HasIndex(a => a.Key).IsUnique();
        builder.Property(a => a.LabelRo).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Category).IsRequired().HasConversion<string>().HasMaxLength(20);
        // Stored as a Postgres integer[] of PropertyType values.
        builder.PrimitiveCollection(a => a.ApplicablePropertyTypes).IsRequired();

        builder.HasData(Seed);
    }
}
