using Imova.Application.Common.Interfaces;
using Imova.Contracts.Locations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Imova.Application.Features.Locations.GetChisinauSectors;

public class GetChisinauSectorsHandler(IApplicationDbContext dbContext, IMemoryCache cache)
    : IRequestHandler<GetChisinauSectorsQuery, List<ChisinauSectorDto>>
{
    private const string CacheKey = "locations:chisinau-sectors";

    public async Task<List<ChisinauSectorDto>> Handle(GetChisinauSectorsQuery request, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out List<ChisinauSectorDto>? cached))
        {
            return cached!;
        }

        var result = await dbContext.ChisinauSectors
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new ChisinauSectorDto(s.Id, s.Name))
            .ToListAsync(cancellationToken);

        // No expiration — see GetRaioaneHandler: seeded once at startup (ChisinauSectorSeeder),
        // only changes via a reseed, which implies a restart anyway.
        cache.Set(CacheKey, result);
        return result;
    }
}
