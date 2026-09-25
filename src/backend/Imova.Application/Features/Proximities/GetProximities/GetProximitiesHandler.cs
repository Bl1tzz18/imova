using Imova.Application.Common.Interfaces;
using Imova.Contracts.Proximities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Imova.Application.Features.Proximities.GetProximities;

public class GetProximitiesHandler(IApplicationDbContext dbContext, IMemoryCache cache)
    : IRequestHandler<GetProximitiesQuery, List<ProximityDto>>
{
    private const string CacheKey = "proximities:all";

    public async Task<List<ProximityDto>> Handle(GetProximitiesQuery request, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out List<ProximityDto>? cached))
        {
            return cached!;
        }

        // Seed order (not alphabetical) — the list is short and ordered roughly by how often
        // people look for each.
        var proximities = await dbContext.Proximities.AsNoTracking().OrderBy(p => p.Id).ToListAsync(cancellationToken);
        var result = proximities.Select(p => p.ToDto()).ToList();

        // No expiration — seeded by a migration and never mutated at runtime (see GetAmenitiesHandler).
        cache.Set(CacheKey, result);
        return result;
    }
}
