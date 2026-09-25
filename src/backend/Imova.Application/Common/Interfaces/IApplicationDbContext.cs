using Imova.Application.Common.Identity;
using Imova.Domain.Amenities;
using Imova.Domain.Favorites;
using Imova.Domain.Listings;
using Imova.Domain.Locations;
using Imova.Domain.Messaging;
using Imova.Domain.Properties;
using Imova.Domain.Proximities;
using Imova.Domain.Publishers;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Property> Properties { get; }

    DbSet<PropertyAmenity> PropertyAmenities { get; }

    DbSet<Amenity> Amenities { get; }

    DbSet<PropertyProximity> PropertyProximities { get; }

    DbSet<Proximity> Proximities { get; }

    DbSet<Listing> Listings { get; }

    DbSet<Publisher> Publishers { get; }

    DbSet<Photo> Photos { get; }

    DbSet<PropertyLocation> PropertyLocations { get; }

    DbSet<Raion> Raioane { get; }

    DbSet<Localitate> Localitati { get; }

    DbSet<ChisinauSector> ChisinauSectors { get; }

    DbSet<Favorite> Favorites { get; }

    DbSet<Conversation> Conversations { get; }

    DbSet<Message> Messages { get; }

    DbSet<UserBlock> UserBlocks { get; }

    DbSet<ConversationReport> ConversationReports { get; }

    DbSet<ApplicationUser> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
