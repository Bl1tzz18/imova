using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Domain.Listings;
using MediatR;

namespace Imova.Application.Features.ListingReports.DismissListingReports;

public class DismissListingReportsHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<DismissListingReportsCommand>
{
    public async Task Handle(DismissListingReportsCommand request, CancellationToken cancellationToken)
    {
        if (!request.IsAdmin)
        {
            throw new ForbiddenAccessException();
        }

        var resolved = await ListingReportResolution.ResolveOpenAsync(
            dbContext, request.ListingId, ListingReportOutcome.Dismissed, request.AdminUserId, request.Note,
            timeProvider.GetUtcNow(), cancellationToken);

        // Usually another admin got there first — say so rather than pretend it worked.
        if (resolved == 0)
        {
            throw new ValidationException(
                [CodedFailure.Of(nameof(request.ListingId), "This listing has no open reports (already handled?).", ErrorCodes.NoOpenReports)]);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
