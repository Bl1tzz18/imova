namespace Imova.Application.Features.Listings.SearchListings;

public static class SearchFilterRules
{
    public const int DefaultPageSize = 24;
    public const int MaxPageSize = 60;

    // The map shows at most this many pins (newest first unless sorted otherwise) — a bound on the
    // response however many listings match; the page says when there are more.
    public const int MaxMapResults = 500;

    public static bool UsesRentalFilters(SearchListingsQuery q) =>
        q.PetsAllowed is not null || q.UtilitiesIncluded is not null || q.MaxLeasePeriodMonths is not null;
}
