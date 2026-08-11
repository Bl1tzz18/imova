using Imova.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Properties;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Price).HasColumnType("numeric(12,2)");
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(3);
        builder.Property(p => p.City).IsRequired().HasMaxLength(100);
        builder.Property(p => p.District).IsRequired().HasMaxLength(100);

        builder.HasData(
            new
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Title = "Apartament cu 2 camere, Botanica",
                Price = 550m,
                Currency = "EUR",
                City = "Chisinau",
                District = "Botanica"
            },
            new
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Title = "Casa cu curte, Durlesti",
                Price = 89000m,
                Currency = "EUR",
                City = "Chisinau",
                District = "Durlesti"
            });
    }
}
