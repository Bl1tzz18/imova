using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Media;
using Imova.Contracts.Media;
using Imova.Contracts.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.GetMyProperties;

public class GetMyPropertiesHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetMyPropertiesQuery, List<PropertyDto>>
{
    public async Task<List<PropertyDto>> Handle(GetMyPropertiesQuery request, CancellationToken cancellationToken)
    {
        var properties = await dbContext.Properties
            .AsNoTracking()
            .Where(p => p.OwnerId == request.OwnerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        if (properties.Count == 0)
        {
            return [];
        }

        var propertyIds = properties.Select(p => p.Id).ToList();

        var locationsByPropertyId = await dbContext.PropertyLocations
            .AsNoTracking()
            .Where(l => propertyIds.Contains(l.PropertyId))
            .ToDictionaryAsync(l => l.PropertyId, cancellationToken);

        var mediaByPropertyId = await dbContext.PropertyMedias
            .AsNoTracking()
            .Where(m => propertyIds.Contains(m.PropertyId))
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        var mediaLookup = mediaByPropertyId
            .GroupBy(m => m.PropertyId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PropertyMediaDto>)g.Select(m => m.ToDto(blobStorageService)).ToList());

        return properties
            .Select(p => p.ToDto(locationsByPropertyId.GetValueOrDefault(p.Id), media: mediaLookup.GetValueOrDefault(p.Id)))
            .ToList();
    }
}
