using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Amenities;
using Imova.Domain.Favorites;
using Imova.Domain.Listings;
using Imova.Domain.Locations;
using Imova.Domain.Messaging;
using Imova.Domain.Properties;
using Imova.Domain.Proximities;
using Imova.Domain.Publishers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Imova.Infrastructure;

// IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid> brings in the standard Identity
// tables (AspNetUsers, AspNetRoles, AspNetUserRoles, …) and already declares a `Users`
// DbSet<ApplicationUser>, which is what satisfies IApplicationDbContext.Users below.
public class ImovaDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    public ImovaDbContext(DbContextOptions<ImovaDbContext> options) : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();

    public DbSet<PropertyAmenity> PropertyAmenities => Set<PropertyAmenity>();

    public DbSet<Amenity> Amenities => Set<Amenity>();

    public DbSet<PropertyProximity> PropertyProximities => Set<PropertyProximity>();

    public DbSet<Proximity> Proximities => Set<Proximity>();

    public DbSet<Listing> Listings => Set<Listing>();

    public DbSet<Publisher> Publishers => Set<Publisher>();

    public DbSet<Photo> Photos => Set<Photo>();

    public DbSet<PropertyLocation> PropertyLocations => Set<PropertyLocation>();

    public DbSet<Raion> Raioane => Set<Raion>();

    public DbSet<Localitate> Localitati => Set<Localitate>();

    public DbSet<ChisinauSector> ChisinauSectors => Set<ChisinauSector>();

    public DbSet<Favorite> Favorites => Set<Favorite>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();

    public DbSet<ConversationReport> ConversationReports => Set<ConversationReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Must run first — this is what configures the Identity entity types.
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImovaDbContext).Assembly);
    }
}
