using Imova.Application.Common.Interfaces;
using Imova.Domain.Locations;
using Imova.Domain.Media;
using Imova.Domain.Properties;
using Imova.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Imova.Infrastructure;

public class ImovaDbContext : DbContext, IApplicationDbContext
{
    public ImovaDbContext(DbContextOptions<ImovaDbContext> options) : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();

    public DbSet<PropertyLocation> PropertyLocations => Set<PropertyLocation>();

    public DbSet<PropertyMedia> PropertyMedias => Set<PropertyMedia>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImovaDbContext).Assembly);
    }
}
