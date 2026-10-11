using Imova.Application.Common;
using Imova.Application.Features.Agencies.Stats;
using Microsoft.EntityFrameworkCore;

namespace Imova.Infrastructure.Agencies;

// One row per agency, Moldovan day and visitor (AgencyPhoneReveal); the key makes the second reveal
// the same day a no-op.
public class AgencyCounters(ImovaDbContext dbContext) : IAgencyCounters
{
    public async Task<bool> TryCountPhoneRevealAsync(Guid agencyId, string visitorHash, DateTimeOffset now, CancellationToken cancellationToken) =>
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "AgencyPhoneReveals" ("AgencyId", "Day", "VisitorHash", "RevealedAt")
            VALUES ({0}, {1}, {2}, {3})
            ON CONFLICT ("AgencyId", "Day", "VisitorHash") DO NOTHING
            """,
            [agencyId, MoldovaTime.Day(now), visitorHash, now],
            cancellationToken) > 0;
}
