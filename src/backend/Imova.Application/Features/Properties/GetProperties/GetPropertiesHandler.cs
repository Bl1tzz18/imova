using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Media;
using Imova.Contracts.Media;
using Imova.Contracts.Properties;
using Imova.Domain.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.GetProperties;

// This backs public browsing only (home page, search, map) — an owner's Draft/Archived listings
// are never meant to appear here regardless of who's asking; see GetMyPropertiesHandler for the
// "show me everything I own, any status" query.
public class GetPropertiesHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetPropertiesQuery, List<PropertyDto>>
{
    public async Task<List<PropertyDto>> Handle(GetPropertiesQuery request, CancellationToken cancellationToken)
    {
        var propertiesQuery = dbContext.Properties.AsNoTracking().Where(p => p.Status == PropertyStatus.Published);

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

        var savedPropertyIds = request.CurrentUserId is null
            ? []
            : await dbContext.Favorites
                .AsNoTracking()
                .Where(f => f.UserId == request.CurrentUserId)
                .Select(f => f.PropertyId)
                .ToHashSetAsync(cancellationToken);

        return properties
            .Select(p => p.ToDto(
                locationsByPropertyId.GetValueOrDefault(p.Id),
                media: mediaLookup.GetValueOrDefault(p.Id),
                isSaved: savedPropertyIds.Contains(p.Id)))
            .ToList();
    }
}
