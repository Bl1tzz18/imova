using Imova.Domain.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Locations;

public class RaionConfiguration : IEntityTypeConfiguration<Raion>
{
    public void Configure(EntityTypeBuilder<Raion> builder)
    {
        builder.ToTable("Raioane");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.SourceId).IsRequired();
        builder.HasIndex(r => r.SourceId).IsUnique();

        builder.Property(r => r.Code).IsRequired().HasMaxLength(4);
        builder.Property(r => r.NameRo).IsRequired().HasMaxLength(100);
        builder.Property(r => r.NameRu).HasMaxLength(100);
        builder.Property(r => r.LocalityLabel).IsRequired().HasConversion<string>().HasMaxLength(20);
    }
}
