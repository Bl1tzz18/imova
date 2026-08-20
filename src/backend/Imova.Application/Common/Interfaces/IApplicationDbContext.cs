using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Property> Properties { get; }

    DbSet<PropertyLocation> PropertyLocations { get; }

    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
