using Imova.Application.Common.Identity;
using Imova.Application.Features.Auth.Sessions;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Auth;

public class RefreshTokenCleanupTests
{
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly ManualTimeProvider _clock = new(new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero));

    private void Add(
        string name, TimeSpan expiresIn, TimeSpan? sessionEndsIn = null, TimeSpan? revokedAgo = null, TimeSpan? replacedAgo = null)
    {
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            TokenHash = name,
            StampFingerprint = "stamp",
            CreatedAt = _clock.Now,
            ExpiresAt = _clock.Now + expiresIn,
            SessionExpiresAt = _clock.Now + (sessionEndsIn ?? TimeSpan.FromDays(60)),
            RevokedAt = _clock.Now - revokedAgo,
            ReplacedAt = _clock.Now - replacedAgo,
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task DeletesOnlyTokensThatCanNeverBeUsedAgain()
    {
        Add("live", TimeSpan.FromDays(10));
        Add("idle-expired", TimeSpan.FromMinutes(-1));
        Add("session-over", TimeSpan.FromDays(10), sessionEndsIn: TimeSpan.FromMinutes(-1));
        Add("revoked-long-ago", TimeSpan.FromDays(10), revokedAgo: TimeSpan.FromDays(2));
        Add("revoked-just-now", TimeSpan.FromDays(10), revokedAgo: TimeSpan.FromMinutes(5));
        // Kept so a replay is still recognised as one.
        Add("replaced", TimeSpan.FromDays(10), replacedAgo: TimeSpan.FromDays(3));

        var deleted = await new RefreshTokenCleanup(_db, _clock).RunAsync(CancellationToken.None);

        Assert.Equal(3, deleted);
        Assert.Equal(
            ["live", "replaced", "revoked-just-now"],
            _db.RefreshTokens.Select(t => t.TokenHash).OrderBy(n => n).ToList());
    }
}
