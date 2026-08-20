using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Imova.Infrastructure;

public class ImovaDbContext : DbContext
{
    public ImovaDbContext(DbContextOptions<ImovaDbContext> options) : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();

    public DbSet<PropertyLocation> PropertyLocations => Set<PropertyLocation>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImovaDbContext).Assembly);
    }
}
