using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
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

    public DbSet<PropertyMedia> PropertyMedias => Set<PropertyMedia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Must run first — this is what configures the Identity entity types.
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImovaDbContext).Assembly);
    }
}
