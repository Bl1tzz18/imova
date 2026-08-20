using Imova.Application.Common.Interfaces;
using Imova.Contracts.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.GetProperties;

public class GetPropertiesHandler(IApplicationDbContext dbContext) : IRequestHandler<GetPropertiesQuery, List<PropertyDto>>
{
    public async Task<List<PropertyDto>> Handle(GetPropertiesQuery request, CancellationToken cancellationToken)
    {
        var properties = await dbContext.Properties
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var locationsByPropertyId = await dbContext.PropertyLocations
            .AsNoTracking()
            .ToDictionaryAsync(l => l.PropertyId, cancellationToken);

        return properties
            .Select(p => p.ToDto(locationsByPropertyId.GetValueOrDefault(p.Id)))
            .ToList();
    }
}
