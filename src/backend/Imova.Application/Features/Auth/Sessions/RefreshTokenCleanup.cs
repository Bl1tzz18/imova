using Imova.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Auth.Sessions;

// One run of the session cleanup job (the Worker runs it periodically): deletes refresh tokens that
// can never be used again — past their idle or absolute limit, or revoked more than a day ago.
// A replaced token is kept until it would have expired, so a replay is still recognised as one.
public class RefreshTokenCleanup(IApplicationDbContext dbContext, TimeProvider timeProvider) : IScheduledJob
{
    public const int BatchSize = 1000;

    // Returns how many tokens were deleted.
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var revokedBefore = now - TimeSpan.FromDays(1);

        var deleted = 0;
        while (true)
        {
            var batch = await dbContext.RefreshTokens
                .Where(t => t.ExpiresAt <= now || t.SessionExpiresAt <= now || t.RevokedAt < revokedBefore)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                return deleted;
            }

            dbContext.RefreshTokens.RemoveRange(batch);
            await dbContext.SaveChangesAsync(cancellationToken);
            deleted += batch.Count;
        }
    }
}
