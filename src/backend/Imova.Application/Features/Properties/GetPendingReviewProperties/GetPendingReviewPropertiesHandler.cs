using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Media;
using Imova.Contracts.Common;
using Imova.Contracts.Media;
using Imova.Contracts.Properties;
using Imova.Domain.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Properties.GetPendingReviewProperties;

public class GetPendingReviewPropertiesHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetPendingReviewPropertiesQuery, PagedResult<PropertyDto>>
{
    public async Task<PagedResult<PropertyDto>> Handle(GetPendingReviewPropertiesQuery request, CancellationToken cancellationToken)
    {
        if (!request.IsAdmin)
        {
            throw new ForbiddenAccessException();
        }

        var pendingReviewQuery = dbContext.Properties
            .AsNoTracking()
            .Where(p => p.Status == PropertyStatus.PendingReview);

        // Oldest submission first — a fair, first-in-first-out review queue.
        var totalCount = await pendingReviewQuery.CountAsync(cancellationToken);
        var properties = await pendingReviewQuery
            .OrderBy(p => p.UpdatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        if (properties.Count == 0)
        {
            return new PagedResult<PropertyDto>([], request.Page, request.PageSize, totalCount);
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

        var items = properties
            .Select(p => p.ToDto(locationsByPropertyId.GetValueOrDefault(p.Id), media: mediaLookup.GetValueOrDefault(p.Id)))
            .ToList();

        return new PagedResult<PropertyDto>(items, request.Page, request.PageSize, totalCount);
    }
}
