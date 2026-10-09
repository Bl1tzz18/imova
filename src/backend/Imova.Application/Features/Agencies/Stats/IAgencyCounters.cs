namespace Imova.Application.Features.Agencies.Stats;

// Counts an agency's own page events for its statistics — for now its phone being revealed: once per
// visitor (hash) and day, written in SQL so two simultaneous reveals by the same person count once.
public interface IAgencyCounters
{
    // True when this reveal was counted (the visitor's first today).
    Task<bool> TryCountPhoneRevealAsync(Guid agencyId, string visitorHash, DateTimeOffset now, CancellationToken cancellationToken);
}
