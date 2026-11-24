using Imova.Application.Common.Interfaces;

namespace Imova.UnitTests.TestSupport;

// Records which accounts were erased from product analytics; Fail makes the call throw.
internal sealed class FakeAnalyticsErasure : IAnalyticsErasure
{
    public List<Guid> Erased { get; } = [];

    public bool Fail { get; set; }

    public Task EraseUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (Fail)
        {
            throw new HttpRequestException("PostHog is down.");
        }
        Erased.Add(userId);
        return Task.CompletedTask;
    }
}
