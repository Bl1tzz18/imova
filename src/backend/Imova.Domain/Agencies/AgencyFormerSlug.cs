namespace Imova.Domain.Agencies;

// A slug an agency used before it was renamed (table AgencySlugHistory). A link to the old slug
// still finds the agency, and the page redirects to the current one — so no other agency may ever
// take a slug that's listed here.
public sealed class AgencyFormerSlug
{
    // For EF Core materialization only.
    private AgencyFormerSlug()
    {
        Slug = string.Empty;
    }

    private AgencyFormerSlug(string slug, Guid agencyId, DateTimeOffset replacedAt)
    {
        Slug = slug;
        AgencyId = agencyId;
        ReplacedAt = replacedAt;
    }

    public string Slug { get; private set; }

    public Guid AgencyId { get; private set; }

    public DateTimeOffset ReplacedAt { get; private set; }

    public static AgencyFormerSlug Create(string slug, Guid agencyId, DateTimeOffset replacedAt) =>
        new(slug, agencyId, replacedAt);
}
