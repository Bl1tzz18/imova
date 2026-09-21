using Imova.Application.Common.Interfaces;
using Imova.Contracts.Locations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Imova.Application.Features.Locations.GetRaioane;

public class GetRaioaneHandler(IApplicationDbContext dbContext, IMemoryCache cache)
    : IRequestHandler<GetRaioaneQuery, List<RaionDto>>
{
    private const string CacheKey = "locations:raioane";

    public async Task<List<RaionDto>> Handle(GetRaioaneQuery request, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out List<RaionDto>? cached))
        {
            return cached!;
        }

        var raioane = await dbContext.Raioane.AsNoTracking().OrderBy(r => r.NameRo).ToListAsync(cancellationToken);

        // Materialize first, then project — Enum.ToString() in the SELECT itself isn't reliably
        // translatable by every EF Core provider; 37 rows makes doing this in-memory a non-issue.
        var result = raioane
            .Select(r => new RaionDto(r.Id, r.Code, r.NameRo, r.NameRu, r.LocalityLabel.ToString()))
            .ToList();

        // No expiration — this is CUATM reference data, seeded idempotently once at startup
        // (CuatmLocationSeeder) and never mutated at runtime, so the only way it can actually
        // change is a reseed, which requires an app restart anyway (clearing this in-memory cache
        // along with it). A time-based TTL would just be periodic no-op re-querying.
        cache.Set(CacheKey, result);
        return result;
    }
}
