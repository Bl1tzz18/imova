using Imova.Application.Common.Interfaces;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.Visitors;

// One run of the Worker's cleanup: deletes visitor marks older than ListingVisitorMark.KeepFor —
// they no longer stop a repeat count, and keeping who-saw-what longer than needed isn't wanted — and
// agency phone reveals older than AgencyPhoneReveal.KeepFor (past what the statistics show).
public class ListingVisitorMarkCleanup(IApplicationDbContext dbContext, TimeProvider timeProvider) : IScheduledJob
{
    public const int BatchSize = 1000;

    // Returns how many rows were deleted.
    public async Task<int> RunAsync(CancellationToken cancellationToken) =>
        await DeleteOldMarksAsync(cancellationToken) + await DeleteOldAgencyRevealsAsync(cancellationToken);

    private async Task<int> DeleteOldAgencyRevealsAsync(CancellationToken cancellationToken)
    {
        var before = timeProvider.GetUtcNow() - AgencyPhoneReveal.KeepFor;
        var deleted = 0;
        while (true)
        {
            var batch = await dbContext.AgencyPhoneReveals
                .Where(r => r.RevealedAt < before)
                .OrderBy(r => r.RevealedAt)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                return deleted;
            }

            dbContext.AgencyPhoneReveals.RemoveRange(batch);
            await dbContext.SaveChangesAsync(cancellationToken);
            deleted += batch.Count;
        }
    }

    private async Task<int> DeleteOldMarksAsync(CancellationToken cancellationToken)
    {
        var before = timeProvider.GetUtcNow() - ListingVisitorMark.KeepFor;
        var deleted = 0;
        while (true)
        {
            var batch = await dbContext.ListingVisitorMarks
                .Where(m => m.CountedAt < before)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                return deleted;
            }

            dbContext.ListingVisitorMarks.RemoveRange(batch);
            await dbContext.SaveChangesAsync(cancellationToken);
            deleted += batch.Count;
        }
    }
}
