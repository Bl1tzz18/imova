using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Admins;
using Imova.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Admins.GetAdminSummary;

// What's waiting for an admin, per admin area: listings to review, listings with open reports,
// open conversation reports and flagged messages nobody has looked at. Drives the count on the
// account menu's "Administrare" entry and on the admin area's tabs. Agencies aren't counted: an
// unverified agency isn't waiting for anything (it may never ask to be verified).
public class GetAdminSummaryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAdminSummaryQuery, AdminSummaryDto>
{
    public async Task<AdminSummaryDto> Handle(GetAdminSummaryQuery request, CancellationToken cancellationToken)
    {
        if (!request.IsAdmin)
        {
            throw new ForbiddenAccessException();
        }

        var pendingReview = await dbContext.Listings.AsNoTracking()
            .CountAsync(l => l.Status == ListingStatus.PendingReview, cancellationToken);
        var reportedListings = await dbContext.ListingReports.AsNoTracking()
            .Where(r => r.ResolvedAt == null)
            .Select(r => r.ListingId)
            .Distinct()
            .CountAsync(cancellationToken);
        var conversationReports = await dbContext.ConversationReports.AsNoTracking()
            .CountAsync(r => r.ResolvedAt == null, cancellationToken);
        var flaggedMessages = await dbContext.Messages.AsNoTracking()
            .CountAsync(m => m.IsFlagged && m.FlagResolvedAt == null, cancellationToken);

        return new AdminSummaryDto(pendingReview, reportedListings, conversationReports, flaggedMessages);
    }
}
