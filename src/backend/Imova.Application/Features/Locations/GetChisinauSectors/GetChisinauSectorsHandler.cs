using Imova.Application.Common.Interfaces;
using Imova.Contracts.Locations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Locations.GetChisinauSectors;

public class GetChisinauSectorsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetChisinauSectorsQuery, List<ChisinauSectorDto>>
{
    public async Task<List<ChisinauSectorDto>> Handle(GetChisinauSectorsQuery request, CancellationToken cancellationToken) =>
        await dbContext.ChisinauSectors
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new ChisinauSectorDto(s.Id, s.Name))
            .ToListAsync(cancellationToken);
}
