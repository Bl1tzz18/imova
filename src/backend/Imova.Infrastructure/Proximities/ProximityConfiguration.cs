using Imova.Domain.Proximities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Proximities;

public class ProximityConfiguration : IEntityTypeConfiguration<Proximity>
{
    // Fixed ids, seeded via HasData so they land in a migration. All apply to every property type.
    public static readonly IReadOnlyList<Proximity> Seed =
    [
        new(Guid.Parse("b1000000-0000-0000-0000-000000000001"), "kindergarten", "Grădiniță"),
        new(Guid.Parse("b1000000-0000-0000-0000-000000000002"), "school", "Școală"),
        new(Guid.Parse("b1000000-0000-0000-0000-000000000003"), "supermarket", "Supermarket / magazin alimentar"),
        new(Guid.Parse("b1000000-0000-0000-0000-000000000004"), "pharmacy", "Farmacie"),
        new(Guid.Parse("b1000000-0000-0000-0000-000000000005"), "public_transport", "Stație transport public"),
        new(Guid.Parse("b1000000-0000-0000-0000-000000000006"), "park", "Parc / zonă verde"),
        new(Guid.Parse("b1000000-0000-0000-0000-000000000007"), "city_center", "Centrul orașului"),
        new(Guid.Parse("b1000000-0000-0000-0000-000000000008"), "hospital", "Spital / policlinică"),
        new(Guid.Parse("b1000000-0000-0000-0000-000000000009"), "farmers_market", "Piață agroalimentară"),
        new(Guid.Parse("b1000000-0000-0000-0000-00000000000a"), "bank", "Bancă / bancomat"),
    ];

    public void Configure(EntityTypeBuilder<Proximity> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Key).IsRequired().HasMaxLength(50);
        builder.HasIndex(p => p.Key).IsUnique();
        builder.Property(p => p.LabelRo).IsRequired().HasMaxLength(100);
        // Stored as a Postgres integer[] of PropertyType values.
        builder.PrimitiveCollection(p => p.ApplicablePropertyTypes).IsRequired();

        builder.HasData(Seed);
    }
}
