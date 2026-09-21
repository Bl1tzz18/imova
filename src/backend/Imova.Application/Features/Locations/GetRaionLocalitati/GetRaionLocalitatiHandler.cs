using Imova.Application.Common.Interfaces;
using Imova.Contracts.Locations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Imova.Application.Features.Locations.GetRaionLocalitati;

public class GetRaionLocalitatiHandler(IApplicationDbContext dbContext, IMemoryCache cache)
    : IRequestHandler<GetRaionLocalitatiQuery, List<LocalitateDto>?>
{
    public async Task<List<LocalitateDto>?> Handle(GetRaionLocalitatiQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"locations:raioane:{request.RaionId}:localitati";
        if (cache.TryGetValue(cacheKey, out List<LocalitateDto>? cached))
        {
            return cached;
        }

        var raionExists = await dbContext.Raioane.AsNoTracking().AnyAsync(r => r.Id == request.RaionId, cancellationToken);
        if (!raionExists)
        {
            // Deliberately not cached: an unknown RaionId is either a client bug or a stale
            // frontend cache, and caching a 404 forever (no expiration, same as the hit path)
            // would keep serving 404 even after a fix, with nothing to invalidate it short of a
            // restart.
            return null;
        }

        var result = await dbContext.Localitati
            .AsNoTracking()
            .Where(l => l.RaionId == request.RaionId)
            .OrderBy(l => l.NameRo)
            .Select(l => new LocalitateDto(l.Id, l.Code, l.NameRo, l.NameRu))
            .ToListAsync(cancellationToken);

        // No expiration — see GetRaioaneHandler: this is seeded-once CUATM reference data that
        // only changes via a reseed, which implies a restart anyway.
        cache.Set(cacheKey, result);
        return result;
    }
}
