namespace Imova.Application.Features.Listings.SearchListings;

public static class SearchFilterRules
{
    public const int DefaultPageSize = 24;
    public const int MaxPageSize = 60;

    public static bool UsesRentalFilters(SearchListingsQuery q) =>
        q.PetsAllowed is not null || q.UtilitiesIncluded is not null || q.MaxLeasePeriodMonths is not null;
}
