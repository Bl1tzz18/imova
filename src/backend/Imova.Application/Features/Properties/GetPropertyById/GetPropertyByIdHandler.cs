using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Media;
using Imova.Contracts.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.GetPropertyById;

public class GetPropertyByIdHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetPropertyByIdQuery, PropertyDto?>
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

        var media = await dbContext.PropertyMedias
            .AsNoTracking()
            .Where(m => m.PropertyId == property.Id)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return property.ToDto(location, owner, media.Select(m => m.ToDto(blobStorageService)).ToList());
    }
}
