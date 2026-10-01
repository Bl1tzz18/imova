using Imova.Application.Common.Interfaces;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.Visitors;

// One run of the Worker's cleanup: deletes visitor marks older than ListingVisitorMark.KeepFor —
// they no longer stop a repeat count, and keeping who-saw-what longer than needed isn't wanted.
public class ListingVisitorMarkCleanup(IApplicationDbContext dbContext, TimeProvider timeProvider) : IScheduledJob
{
    public const int BatchSize = 1000;

    // Returns how many marks were deleted.
    public async Task<int> RunAsync(CancellationToken cancellationToken)
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
