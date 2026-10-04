using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Agencies;
using Imova.Domain.Amenities;
using Imova.Domain.Favorites;
using Imova.Domain.Listings;
using Imova.Domain.Locations;
using Imova.Domain.Messaging;
using Imova.Domain.Properties;
using Imova.Domain.Proximities;
using Imova.Domain.Publishers;
using Imova.Domain.SavedSearches;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Imova.Infrastructure;

// IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid> brings in the standard Identity
// tables (AspNetUsers, AspNetRoles, AspNetUserRoles, …) and already declares a `Users`
// DbSet<ApplicationUser>, which is what satisfies IApplicationDbContext.Users below.
public class ImovaDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext, IDataProtectionKeyContext
{
    public ImovaDbContext(DbContextOptions<ImovaDbContext> options) : base(options)
    {
    }

    // The ASP.NET Core data-protection key ring (see AddDataProtection in Program.cs).
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<Property> Properties => Set<Property>();

    public DbSet<PropertyAmenity> PropertyAmenities => Set<PropertyAmenity>();

    public DbSet<Amenity> Amenities => Set<Amenity>();

    public DbSet<PropertyProximity> PropertyProximities => Set<PropertyProximity>();

    public DbSet<Proximity> Proximities => Set<Proximity>();

    public DbSet<Listing> Listings => Set<Listing>();

    public DbSet<Publisher> Publishers => Set<Publisher>();

    public DbSet<Agency> Agencies => Set<Agency>();

    public DbSet<AgencyMember> AgencyMembers => Set<AgencyMember>();

    public DbSet<AgencyFormerSlug> AgencyFormerSlugs => Set<AgencyFormerSlug>();

    public DbSet<Photo> Photos => Set<Photo>();

    public DbSet<PropertyLocation> PropertyLocations => Set<PropertyLocation>();

    public DbSet<Raion> Raioane => Set<Raion>();

    public DbSet<Localitate> Localitati => Set<Localitate>();

    public DbSet<ChisinauSector> ChisinauSectors => Set<ChisinauSector>();

    public DbSet<Favorite> Favorites => Set<Favorite>();

    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();

    public DbSet<ConversationReport> ConversationReports => Set<ConversationReport>();

    public DbSet<ListingReport> ListingReports => Set<ListingReport>();

    public DbSet<ListingPriceChange> ListingPriceChanges => Set<ListingPriceChange>();

    public DbSet<ListingVisitorMark> ListingVisitorMarks => Set<ListingVisitorMark>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AdminAuditEntry> AdminAuditEntries => Set<AdminAuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Must run first — this is what configures the Identity entity types.
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImovaDbContext).Assembly);
    }
}
