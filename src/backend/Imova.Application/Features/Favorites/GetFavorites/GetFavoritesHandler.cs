using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Media;
using Imova.Application.Features.Properties;
using Imova.Contracts.Media;
using Imova.Contracts.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Favorites.GetFavorites;

public class GetFavoritesHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetFavoritesQuery, List<PropertyDto>>
{
    public async Task<List<PropertyDto>> Handle(GetFavoritesQuery request, CancellationToken cancellationToken)
    {
        var favoritePropertyIds = await dbContext.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == request.UserId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.PropertyId)
            .ToListAsync(cancellationToken);

        if (favoritePropertyIds.Count == 0)
        {
            return [];
        }

        var propertiesById = await dbContext.Properties
            .AsNoTracking()
            .Where(p => favoritePropertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var locationsByPropertyId = await dbContext.PropertyLocations
            .AsNoTracking()
            .Where(l => favoritePropertyIds.Contains(l.PropertyId))
            .ToDictionaryAsync(l => l.PropertyId, cancellationToken);

        var mediaByPropertyId = await dbContext.PropertyMedias
            .AsNoTracking()
            .Where(m => favoritePropertyIds.Contains(m.PropertyId))
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        var mediaLookup = mediaByPropertyId
            .GroupBy(m => m.PropertyId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PropertyMediaDto>)g.Select(m => m.ToDto(blobStorageService)).ToList());

        // Preserve the "most recently saved first" order — Properties.Where(...) makes no
        // ordering guarantee of its own.
        return favoritePropertyIds
            .Where(propertiesById.ContainsKey)
            .Select(id => propertiesById[id].ToDto(
                locationsByPropertyId.GetValueOrDefault(id),
                media: mediaLookup.GetValueOrDefault(id),
                isSaved: true))
            .ToList();
    }
}
