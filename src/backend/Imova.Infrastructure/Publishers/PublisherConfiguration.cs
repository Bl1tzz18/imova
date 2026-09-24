using Imova.Application.Common.Identity;
using Imova.Domain.Publishers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Publishers;

public class PublisherConfiguration : IEntityTypeConfiguration<Publisher>
{
    public void Configure(EntityTypeBuilder<Publisher> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);

        // At most one Individual and one Agency publisher per user.
        builder.HasIndex(p => new { p.UserId, p.PublisherType }).IsUnique();

        builder.Property(p => p.PublisherType).IsRequired();
        builder.Property(p => p.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Phone).HasMaxLength(20);
        builder.Property(p => p.Email).IsRequired().HasMaxLength(256);
        builder.Property(p => p.LogoUrl).HasMaxLength(500);
        builder.Property(p => p.Bio).HasMaxLength(2000);
    }
}
