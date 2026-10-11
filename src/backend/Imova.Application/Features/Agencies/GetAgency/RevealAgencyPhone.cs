using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Agencies.Stats;
using Imova.Application.Features.Listings.Visitors;
using Imova.Contracts.Agencies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.GetAgency;

// "Arată numărul" on an agency's page: the only way the public gets its full number (the page only
// has its shape, AgencyDto.PhonePrefix/PhoneHiddenDigits), one request at a time and rate-limited
// per IP together with listings' numbers. Null (404) when the caller may not see the agency.
// Counted for the agency's statistics once per visitor and day (IAgencyCounters) — not when one of
// its own members looks.
public record RevealAgencyPhoneQuery(Guid AgencyId, Guid? UserId, bool IsAdmin, string? VisitorId = null) : IRequest<AgencyPhoneDto?>;

public class RevealAgencyPhoneHandler(IApplicationDbContext dbContext, IAgencyCounters counters, TimeProvider timeProvider)
    : IRequestHandler<RevealAgencyPhoneQuery, AgencyPhoneDto?>
{
    public async Task<AgencyPhoneDto?> Handle(RevealAgencyPhoneQuery request, CancellationToken cancellationToken)
    {
        var agency = await dbContext.Agencies.AsNoTracking()
            .Include(a => a.Members)
            .FirstOrDefaultAsync(a => a.Id == request.AgencyId, cancellationToken);

        if (agency is null || !AgencyAccess.CanView(agency, request.UserId, request.IsAdmin) || string.IsNullOrWhiteSpace(agency.Phone))
        {
            return null;
        }

        var isMember = request.UserId is { } userId && agency.IsMember(userId);
        if (!isMember && ListingVisitors.Hash(request.UserId, request.VisitorId) is { } visitor)
        {
            await counters.TryCountPhoneRevealAsync(agency.Id, visitor, timeProvider.GetUtcNow(), cancellationToken);
        }

        return new AgencyPhoneDto(agency.Phone);
    }
}
