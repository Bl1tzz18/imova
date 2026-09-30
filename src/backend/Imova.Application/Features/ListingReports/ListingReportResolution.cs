using Imova.Application.Common.Interfaces;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.ListingReports;

// An admin's decision on a reported listing closes every report that is open on it, with the same
// outcome, admin, time and note — together they're one case in the history. Used by the queue's
// "dismiss" and by every suspension (from the reports tab or the Active tab alike). Doesn't save.
public static class ListingReportResolution
{
    public static async Task<int> ResolveOpenAsync(
        IApplicationDbContext dbContext,
        Guid listingId,
        ListingReportOutcome outcome,
        Guid adminUserId,
        string? note,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var open = await dbContext.ListingReports
            .Where(r => r.ListingId == listingId && r.ResolvedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var report in open)
        {
            report.Resolve(outcome, adminUserId, note, now);
        }

        return open.Count;
    }
}
