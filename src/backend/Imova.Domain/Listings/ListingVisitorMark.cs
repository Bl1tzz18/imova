namespace Imova.Domain.Listings;

// What a listing's counters count.
public enum ListingCounter
{
    View = 1,
    PhoneReveal = 2,
}

// "This visitor was last counted for this listing at …" — so the same person opening a listing (or
// its phone number) again within 24 hours isn't counted again. The visitor is only a hash (of their
// account id, or of the anonymous visitor cookie), and a mark is deleted after two days.
public sealed class ListingVisitorMark
{
    // Counted again only after this long.
    public static readonly TimeSpan CountOncePer = TimeSpan.FromHours(24);

    // Older marks no longer stop anything and are deleted (ListingVisitorMarkCleanup).
    public static readonly TimeSpan KeepFor = TimeSpan.FromDays(2);

    // For EF Core materialization only.
    private ListingVisitorMark()
    {
        VisitorHash = string.Empty;
    }

    public Guid ListingId { get; private set; }

    public ListingCounter Counter { get; private set; }

    // Lower-case hex SHA-256 — see ListingVisitors.
    public string VisitorHash { get; private set; }

    public DateTimeOffset CountedAt { get; private set; }
}
