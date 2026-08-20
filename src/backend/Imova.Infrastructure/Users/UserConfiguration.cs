using Imova.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Users;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Phone).HasMaxLength(32);

        builder.HasData(new
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Email = "demo@imova.md",
            Phone = (string?)null,
            CreatedAt = SeedData.SeedDate,
            LastLoginAt = (DateTimeOffset?)null
        });
    }
}
