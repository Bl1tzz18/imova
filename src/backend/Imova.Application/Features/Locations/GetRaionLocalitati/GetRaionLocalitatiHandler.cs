using Imova.Application.Common.Interfaces;
using Imova.Contracts.Locations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Locations.GetRaionLocalitati;

public class GetRaionLocalitatiHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetRaionLocalitatiQuery, List<LocalitateDto>?>
{
    public async Task<List<LocalitateDto>?> Handle(GetRaionLocalitatiQuery request, CancellationToken cancellationToken)
    {
        var raionExists = await dbContext.Raioane.AsNoTracking().AnyAsync(r => r.Id == request.RaionId, cancellationToken);
        if (!raionExists)
        {
            return null;
        }

        return await dbContext.Localitati
            .AsNoTracking()
            .Where(l => l.RaionId == request.RaionId)
            .OrderBy(l => l.NameRo)
            .Select(l => new LocalitateDto(l.Id, l.Code, l.NameRo, l.NameRu))
            .ToListAsync(cancellationToken);
    }
}
