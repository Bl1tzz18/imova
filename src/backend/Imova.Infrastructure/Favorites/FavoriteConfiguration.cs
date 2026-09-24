using Imova.Application.Common.Identity;
using Imova.Domain.Favorites;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Favorites;

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.UserId).IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(f => f.ListingId).IsRequired();
        builder.HasOne<Listing>().WithMany().HasForeignKey(f => f.ListingId).OnDelete(DeleteBehavior.Cascade);

        // The same listing can't be saved twice by the same user.
        builder.HasIndex(f => new { f.UserId, f.ListingId }).IsUnique();
    }
}
