using Imova.Application.Common.Interfaces;
using Imova.Contracts.Locations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Locations.GetRaioane;

public class GetRaioaneHandler(IApplicationDbContext dbContext) : IRequestHandler<GetRaioaneQuery, List<RaionDto>>
{
    public async Task<List<RaionDto>> Handle(GetRaioaneQuery request, CancellationToken cancellationToken)
    {
        var raioane = await dbContext.Raioane.AsNoTracking().OrderBy(r => r.NameRo).ToListAsync(cancellationToken);

        // Materialize first, then project — Enum.ToString() in the SELECT itself isn't reliably
        // translatable by every EF Core provider; 37 rows makes doing this in-memory a non-issue.
        return raioane
            .Select(r => new RaionDto(r.Id, r.Code, r.NameRo, r.NameRu, r.LocalityLabel.ToString()))
            .ToList();
    }
}
