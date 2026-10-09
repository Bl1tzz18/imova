namespace Imova.Domain.Agencies;

// Someone revealed an agency's phone number on its page, on one day (Europe/Chisinau). One row per
// agency, day and visitor (a SHA-256 of their account id or anonymous cookie — never an IP), so a
// visitor counts once a day; the day's count is the number of rows. Written in SQL (INSERT … ON
// CONFLICT DO NOTHING) by IAgencyCounters; kept AgencyPhoneReveal.KeepFor for the statistics.
public sealed class AgencyPhoneReveal
{
    // The statistics reach back at most a year.
    public static readonly TimeSpan KeepFor = TimeSpan.FromDays(400);

    private AgencyPhoneReveal()
    {
        VisitorHash = null!;
    }

    public Guid AgencyId { get; private set; }

    public DateOnly Day { get; private set; }

    public string VisitorHash { get; private set; }

    public DateTimeOffset RevealedAt { get; private set; }

    // For tests and seeding — the API writes these rows in SQL.
    public static AgencyPhoneReveal Create(Guid agencyId, DateOnly day, string visitorHash, DateTimeOffset revealedAt) =>
        new() { AgencyId = agencyId, Day = day, VisitorHash = visitorHash, RevealedAt = revealedAt };
}
