using Imova.Domain.Properties;
using Microsoft.EntityFrameworkCore;

namespace Imova.Infrastructure;

public class ImovaDbContext : DbContext
{
    public ImovaDbContext(DbContextOptions<ImovaDbContext> options) : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImovaDbContext).Assembly);
    }
}
