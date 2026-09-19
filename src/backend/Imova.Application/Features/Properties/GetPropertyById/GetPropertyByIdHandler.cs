using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Media;
using Imova.Contracts.Properties;
using Imova.Domain.Properties;
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

        // A non-Published listing is only visible to its own owner (e.g. the "my listings" edit
        // page) or an admin (e.g. reviewing it from the moderation queue) — anyone else, including
        // an anonymous visitor, gets the same "not found" as if the row didn't exist. Matches
        // GetPropertiesHandler's public-browsing filter.
        var isOwner = request.CurrentUserId is not null && request.CurrentUserId == property.OwnerId;
        if (property.Status != PropertyStatus.Published && !isOwner && !request.IsAdmin)
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

        var isSaved = request.CurrentUserId is not null && await dbContext.Favorites
            .AsNoTracking()
            .AnyAsync(f => f.UserId == request.CurrentUserId && f.PropertyId == property.Id, cancellationToken);

        return property.ToDto(location, owner, media.Select(m => m.ToDto(blobStorageService)).ToList(), isSaved);
    }
}
