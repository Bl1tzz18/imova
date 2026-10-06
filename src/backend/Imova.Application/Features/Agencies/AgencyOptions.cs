namespace Imova.Application.Features.Agencies;

// Bound from the "Agencies" configuration section.
public class AgencyOptions
{
    public const string SectionName = "Agencies";

    // How many agencies one account may own (be an Owner of) — creating one more is refused
    // (agency.limitReached). One setting for every account.
    public int MaxOwnedPerUser { get; set; } = 3;
}
