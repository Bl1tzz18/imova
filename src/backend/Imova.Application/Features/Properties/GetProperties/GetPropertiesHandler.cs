using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Media;
using Imova.Contracts.Media;
using Imova.Contracts.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.GetProperties;

public class GetPropertiesHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetPropertiesQuery, List<PropertyDto>>
{
    public async Task<List<PropertyDto>> Handle(GetPropertiesQuery request, CancellationToken cancellationToken)
    {
        var properties = await dbContext.Properties
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var locationsByPropertyId = await dbContext.PropertyLocations
            .AsNoTracking()
            .ToDictionaryAsync(l => l.PropertyId, cancellationToken);

        var mediaByPropertyId = await dbContext.PropertyMedias
            .AsNoTracking()
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
