namespace Imova.Domain.Listings;

// One listing's counted views and phone reveals on one day (the Europe/Chisinau calendar day) — the
// statistics' daily series. Written only by IListingCounters, in the same SQL statement that raises
// the listing's own totals (ViewCount / PhoneRevealCount), so the two never disagree; read for the
// agency statistics. Messages and favorites per day come from their own tables.
public sealed class ListingDailyStat
{
    private ListingDailyStat()
    {
    }

    public Guid ListingId { get; private set; }

    public DateOnly Day { get; private set; }

    public int Views { get; private set; }

    public int PhoneReveals { get; private set; }

    // For tests and seeding — the API writes these rows in SQL.
    public static ListingDailyStat Create(Guid listingId, DateOnly day, int views, int phoneReveals) =>
        new() { ListingId = listingId, Day = day, Views = views, PhoneReveals = phoneReveals };
}
