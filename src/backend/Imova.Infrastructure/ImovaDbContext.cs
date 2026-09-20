using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Favorites;
using Imova.Domain.Locations;
using Imova.Domain.Media;
using Imova.Domain.Properties;
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

    public DbSet<PropertyLocation> PropertyLocations => Set<PropertyLocation>();

    public DbSet<Raion> Raioane => Set<Raion>();

    public DbSet<Localitate> Localitati => Set<Localitate>();

    public DbSet<ChisinauSector> ChisinauSectors => Set<ChisinauSector>();

    public DbSet<PropertyMedia> PropertyMedias => Set<PropertyMedia>();

    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Must run first — this is what configures the Identity entity types.
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImovaDbContext).Assembly);
    }
}
