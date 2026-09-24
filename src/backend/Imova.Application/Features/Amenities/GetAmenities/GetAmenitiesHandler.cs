using Imova.Application.Common.Interfaces;
using Imova.Contracts.Amenities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Imova.Application.Features.Amenities.GetAmenities;

public class GetAmenitiesHandler(IApplicationDbContext dbContext, IMemoryCache cache)
    : IRequestHandler<GetAmenitiesQuery, List<AmenityDto>>
{
    private const string CacheKey = "amenities:all";

    public async Task<List<AmenityDto>> Handle(GetAmenitiesQuery request, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out List<AmenityDto>? cached))
        {
            return cached!;
        }

        var amenities = await dbContext.Amenities.AsNoTracking().OrderBy(a => a.LabelRo).ToListAsync(cancellationToken);
        var result = amenities.Select(a => a.ToDto()).ToList();

        // No expiration — amenities are seeded by a migration (AmenityConfiguration's HasData) and
        // never mutated at runtime, so they can only change with a deploy/restart, which clears
        // this in-memory cache anyway. Same reasoning as GetRaioaneHandler.
        cache.Set(CacheKey, result);
        return result;
    }
}
