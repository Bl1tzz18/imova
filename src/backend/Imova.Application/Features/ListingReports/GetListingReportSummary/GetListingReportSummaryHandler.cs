using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.ListingReports.GetListingReportSummary;

public class GetListingReportSummaryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetListingReportSummaryQuery, ListingReportSummaryDto>
{
    public async Task<ListingReportSummaryDto> Handle(GetListingReportSummaryQuery request, CancellationToken cancellationToken)
    {
        if (!request.IsAdmin)
        {
            throw new ForbiddenAccessException();
        }

        var open = dbContext.ListingReports.AsNoTracking().Where(r => r.ResolvedAt == null);
        return new ListingReportSummaryDto(
            await open.Select(r => r.ListingId).Distinct().CountAsync(cancellationToken),
            await open.CountAsync(cancellationToken));
    }
}
