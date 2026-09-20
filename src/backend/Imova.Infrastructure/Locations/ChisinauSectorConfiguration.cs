using Imova.Domain.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Locations;

public class ChisinauSectorConfiguration : IEntityTypeConfiguration<ChisinauSector>
{
    public void Configure(EntityTypeBuilder<ChisinauSector> builder)
    {
        builder.ToTable("ChisinauSectors");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.Name).IsUnique();
    }
}
