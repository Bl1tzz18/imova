using Imova.Application.Features.Agencies.Stats;

namespace Imova.UnitTests.TestSupport;

// Records what would be counted (the real one is SQL, covered by the integration tests).
internal sealed class FakeAgencyCounters : IAgencyCounters
{
    public List<(Guid AgencyId, string VisitorHash)> PhoneReveals { get; } = [];

    public Task<bool> TryCountPhoneRevealAsync(Guid agencyId, string visitorHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        PhoneReveals.Add((agencyId, visitorHash));
        return Task.FromResult(true);
    }
}
