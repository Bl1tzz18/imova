using Imova.Application.Common.Interfaces;
using Imova.Contracts.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.GetPropertyById;

public class GetPropertyByIdHandler(IApplicationDbContext dbContext) : IRequestHandler<GetPropertyByIdQuery, PropertyDto?>
{
    public async Task<PropertyDto?> Handle(GetPropertyByIdQuery request, CancellationToken cancellationToken)
    {
        var property = await dbContext.Properties
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (property is null)
        {
            return null;
        }

        var location = await dbContext.PropertyLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.PropertyId == property.Id, cancellationToken);

        var owner = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == property.OwnerId, cancellationToken);

        return property.ToDto(location, owner);
    }
}
