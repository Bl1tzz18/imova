using Imova.Application.Common.Interfaces;
using Imova.Contracts.Agencies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.GetAgency;

// "Arată numărul" on an agency's page: the only way the public gets its full number (the page only
// has its shape, AgencyDto.PhonePrefix/PhoneHiddenDigits), one request at a time and rate-limited
// per IP together with listings' numbers. Null (404) when the caller may not see the agency.
// Not counted anywhere yet — agency statistics come later (PR 3).
public record RevealAgencyPhoneQuery(Guid AgencyId, Guid? UserId, bool IsAdmin) : IRequest<AgencyPhoneDto?>;

public class RevealAgencyPhoneHandler(IApplicationDbContext dbContext) : IRequestHandler<RevealAgencyPhoneQuery, AgencyPhoneDto?>
{
    public async Task<AgencyPhoneDto?> Handle(RevealAgencyPhoneQuery request, CancellationToken cancellationToken)
    {
        var agency = await dbContext.Agencies.AsNoTracking()
            .Include(a => a.Members)
            .FirstOrDefaultAsync(a => a.Id == request.AgencyId, cancellationToken);

        return agency is null || !AgencyAccess.CanView(agency, request.UserId, request.IsAdmin) || string.IsNullOrWhiteSpace(agency.Phone)
            ? null
            : new AgencyPhoneDto(agency.Phone);
    }
}
