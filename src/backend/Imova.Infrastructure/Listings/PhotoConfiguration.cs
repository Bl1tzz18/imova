using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Listings;

public class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> builder)
    {
        builder.ToTable("Photos");
        builder.HasKey(p => p.Id);

        // No FK to Listing — see the comment on Photo for why (the upload flow can create rows
        // before the Listing they belong to exists).
        builder.Property(p => p.ListingId).IsRequired();
        builder.HasIndex(p => p.ListingId);

        builder.Property(p => p.BlobName).IsRequired().HasMaxLength(500);
        builder.HasIndex(p => p.BlobName).IsUnique();

        builder.Property(p => p.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(p => p.ModerationStatus).IsRequired();
        builder.Property(p => p.IsPrimary).IsRequired();
    }
}
