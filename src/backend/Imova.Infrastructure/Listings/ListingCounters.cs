using Imova.Application.Features.Listings.Visitors;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;

namespace Imova.Infrastructure.Listings;

// One statement: record (or refresh) the visitor's mark — refreshing only one older than
// ListingVisitorMark.CountOncePer — and raise the counter only if the mark was written. Postgres runs
// the data-modifying CTE once whatever the UPDATE does, and the unique key on the mark makes two
// simultaneous visits by the same person count once.
public class ListingCounters(ImovaDbContext dbContext) : IListingCounters
{
    public async Task<bool> TryCountAsync(
        Guid listingId, ListingCounter counter, string visitorHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // A fixed column name per counter — never anything from the request.
        var column = counter switch
        {
            ListingCounter.View => "ViewCount",
            ListingCounter.PhoneReveal => "PhoneRevealCount",
            _ => throw new ArgumentOutOfRangeException(nameof(counter)),
        };

#pragma warning disable EF1002 // Only the fixed column name is interpolated; every value is a parameter.
        var updated = await dbContext.Database.ExecuteSqlRawAsync(
            $$"""
            WITH counted AS (
                INSERT INTO "ListingVisitorMarks" ("ListingId", "Counter", "VisitorHash", "CountedAt")
                VALUES ({0}, {1}, {2}, {3})
                ON CONFLICT ("ListingId", "Counter", "VisitorHash")
                DO UPDATE SET "CountedAt" = EXCLUDED."CountedAt"
                WHERE "ListingVisitorMarks"."CountedAt" <= {4}
                RETURNING 1
            )
            UPDATE "Listings" SET "{{column}}" = "{{column}}" + 1
            WHERE "Id" = {0} AND EXISTS (SELECT 1 FROM counted)
            """,
            [listingId, (int)counter, visitorHash, now, now - ListingVisitorMark.CountOncePer],
            cancellationToken);
#pragma warning restore EF1002

        return updated > 0;
    }
}
