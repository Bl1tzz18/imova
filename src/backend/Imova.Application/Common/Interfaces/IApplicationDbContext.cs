using Imova.Application.Common.Identity;
using Imova.Domain.Favorites;
using Imova.Domain.Locations;
using Imova.Domain.Media;
using Imova.Domain.Properties;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Property> Properties { get; }

    DbSet<PropertyLocation> PropertyLocations { get; }

    DbSet<PropertyMedia> PropertyMedias { get; }

    DbSet<Favorite> Favorites { get; }

    DbSet<ApplicationUser> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
