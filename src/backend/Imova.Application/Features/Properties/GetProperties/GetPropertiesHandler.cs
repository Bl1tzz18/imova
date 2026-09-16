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
        var propertiesQuery = dbContext.Properties.AsNoTracking();

        if (request.PropertyType is not null)
        {
            propertiesQuery = propertiesQuery.Where(p => p.PropertyType == request.PropertyType);
        }

        var properties = await propertiesQuery.ToListAsync(cancellationToken);

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
