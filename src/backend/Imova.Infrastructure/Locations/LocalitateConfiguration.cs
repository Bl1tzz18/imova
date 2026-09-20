using Imova.Domain.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Locations;

public class LocalitateConfiguration : IEntityTypeConfiguration<Localitate>
{
    public void Configure(EntityTypeBuilder<Localitate> builder)
    {
        builder.ToTable("Localitati");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.SourceId).IsRequired();
        builder.HasIndex(l => l.SourceId).IsUnique();

        builder.Property(l => l.RaionId).IsRequired();
        builder.HasIndex(l => l.RaionId);
        builder.HasOne<Raion>().WithMany().HasForeignKey(l => l.RaionId).OnDelete(DeleteBehavior.Restrict);

        // Self-referencing true CUATM parent chain — data fidelity only, see Localitate.cs.
        builder.HasOne<Localitate>().WithMany().HasForeignKey(l => l.ParentLocalityId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.Code).IsRequired().HasMaxLength(4);
        // Some flattened compound names (e.g. a Chișinău suburb's own sub-village) run longer than
        // a typical locality name.
        builder.Property(l => l.NameRo).IsRequired().HasMaxLength(150);
        builder.Property(l => l.NameRu).HasMaxLength(150);
    }
}
