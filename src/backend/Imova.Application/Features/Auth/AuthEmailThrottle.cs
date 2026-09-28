namespace Imova.Application.Features.Auth;

// At most one account email (reset link, confirmation link) per key per Cooldown, so the
// "send me a link" endpoints can't be used to flood someone's inbox. The per-IP rate limit on
// those endpoints doesn't cover that: it limits the sender, this limits the recipient.
// In memory, per API instance — like messaging presence (see CLAUDE.md).
public class AuthEmailThrottle(TimeProvider timeProvider)
{
    public static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(1);

    private const int PruneThreshold = 10_000;

    private readonly Dictionary<string, DateTimeOffset> _lastSentAt = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();

    // True (and the key's cooldown starts) when an email may be sent now.
    public bool TryAcquire(string key)
    {
        var now = timeProvider.GetUtcNow();
        lock (_lock)
        {
            if (_lastSentAt.TryGetValue(key, out var last) && now - last < Cooldown)
            {
                return false;
            }

            if (_lastSentAt.Count >= PruneThreshold)
            {
                foreach (var stale in _lastSentAt.Where(kv => now - kv.Value >= Cooldown).Select(kv => kv.Key).ToList())
                {
                    _lastSentAt.Remove(stale);
                }
            }

            _lastSentAt[key] = now;
            return true;
        }
    }
}
