using Imova.Contracts.Properties;
using Imova.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.GetProperties;

public class GetPropertiesHandler(ImovaDbContext dbContext) : IRequestHandler<GetPropertiesQuery, List<PropertyDto>>
{
    public async Task<List<PropertyDto>> Handle(GetPropertiesQuery request, CancellationToken cancellationToken)
    {
        var properties = await dbContext.Properties
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return properties.Select(p => p.ToDto()).ToList();
    }
}
