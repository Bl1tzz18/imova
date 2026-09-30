namespace Imova.Application.Features.Listings;

// Bound from the "ListingReports" configuration section.
public class ListingReportOptions
{
    public const string SectionName = "ListingReports";

    // Anti-abuse: how many listings one user may report per rolling 24 hours (amending an open
    // report doesn't count). Counted in the database, so it holds across API instances.
    public int MaxPerDay { get; set; } = 10;
}
