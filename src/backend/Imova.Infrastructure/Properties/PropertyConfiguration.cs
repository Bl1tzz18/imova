using Imova.Application.Common.Identity;
using Imova.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Properties;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.OwnerId).IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(p => p.OwnerId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(4000);
        builder.Property(p => p.PropertyType).IsRequired();
        builder.Property(p => p.ListingType).IsRequired();
        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.Price).HasColumnType("numeric(12,2)");
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(3);
        builder.Property(p => p.Area).HasColumnType("numeric(8,2)");
        builder.Property(p => p.Rooms).HasColumnType("numeric(4,1)");

        builder.HasIndex(p => p.OwnerId);
        builder.HasIndex(p => p.Status);
    }
}
