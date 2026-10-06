using Imova.Application.Common.Interfaces;
using Imova.Contracts.Agencies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.GetAgency;

public class GetAgencyHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetAgencyQuery, AgencyDto?>
{
    public async Task<AgencyDto?> Handle(GetAgencyQuery request, CancellationToken cancellationToken)
    {
        var agency = await dbContext.Agencies.AsNoTracking()
            .Include(a => a.Members)
            .FirstOrDefaultAsync(a => a.Id == request.AgencyId, cancellationToken);

        if (agency is null || !AgencyAccess.CanView(agency, request.UserId, request.IsAdmin))
        {
            return null;
        }

        return await AgencyDtoLoader.LoadAsync(dbContext, blobStorageService, agency, request.UserId, request.IsAdmin, cancellationToken);
    }
}
