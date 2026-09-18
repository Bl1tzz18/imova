using Imova.Application.Common.Identity;
using Imova.Domain.Favorites;
using Imova.Domain.Properties;
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

        builder.Property(f => f.PropertyId).IsRequired();
        builder.HasOne<Property>().WithMany().HasForeignKey(f => f.PropertyId).OnDelete(DeleteBehavior.Cascade);

        // The same listing can't be saved twice by the same user.
        builder.HasIndex(f => new { f.UserId, f.PropertyId }).IsUnique();
    }
}
