using Imova.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Media;

public class PropertyMediaConfiguration : IEntityTypeConfiguration<PropertyMedia>
{
    public void Configure(EntityTypeBuilder<PropertyMedia> builder)
    {
        builder.HasKey(m => m.Id);

        // No FK to Property — see the comment on PropertyMedia for why (the upload flow can
        // create rows before the Property they belong to exists).
        builder.Property(m => m.PropertyId).IsRequired();
        builder.HasIndex(m => m.PropertyId);

        builder.Property(m => m.BlobName).IsRequired().HasMaxLength(500);
        builder.HasIndex(m => m.BlobName).IsUnique();

        builder.Property(m => m.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(m => m.ModerationStatus).IsRequired();
    }
}
