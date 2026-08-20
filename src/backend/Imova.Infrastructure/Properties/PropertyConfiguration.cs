using Imova.Domain.Properties;
using Imova.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Properties;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.OwnerId).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(p => p.OwnerId).OnDelete(DeleteBehavior.Restrict);

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

        builder.HasData(
            new
            {
                Id = SeedData.Property1Id,
                OwnerId = SeedData.DemoOwnerId,
                OrganizationId = (Guid?)null,
                Title = "Apartament cu 2 camere, Botanica",
                Description = "Apartament luminos, renovat recent, aproape de centrul orasului.",
                PropertyType = Domain.Properties.PropertyType.Apartment,
                ListingType = Domain.Properties.ListingType.Rent,
                Status = Domain.Properties.PropertyStatus.Published,
                Price = 550m,
                Currency = "EUR",
                Area = (decimal?)null,
                Rooms = (decimal?)null,
                Bathrooms = (short?)null,
                Floor = (short?)null,
                TotalFloors = (short?)null,
                YearBuilt = (short?)null,
                Furnished = (bool?)null,
                ParkingAvailable = (bool?)null,
                PetsAllowed = (bool?)null,
                CreatedAt = SeedData.SeedDate,
                UpdatedAt = SeedData.SeedDate,
                PublishedAt = SeedData.SeedDate,
                ExpiresAt = (DateTimeOffset?)null
            },
            new
            {
                Id = SeedData.Property2Id,
                OwnerId = SeedData.DemoOwnerId,
                OrganizationId = (Guid?)null,
                Title = "Casa cu curte, Durlesti",
                Description = "Casa spatioasa cu curte proprie, ideala pentru o familie.",
                PropertyType = Domain.Properties.PropertyType.House,
                ListingType = Domain.Properties.ListingType.Sale,
                Status = Domain.Properties.PropertyStatus.Published,
                Price = 89000m,
                Currency = "EUR",
                Area = (decimal?)null,
                Rooms = (decimal?)null,
                Bathrooms = (short?)null,
                Floor = (short?)null,
                TotalFloors = (short?)null,
                YearBuilt = (short?)null,
                Furnished = (bool?)null,
                ParkingAvailable = (bool?)null,
                PetsAllowed = (bool?)null,
                CreatedAt = SeedData.SeedDate,
                UpdatedAt = SeedData.SeedDate,
                PublishedAt = SeedData.SeedDate,
                ExpiresAt = (DateTimeOffset?)null
            });
    }
}
