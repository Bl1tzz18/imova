using FluentValidation;
using Imova.Application.Features.Listings.SearchListings;
using Imova.Contracts.SavedSearches;
using Imova.Domain.SavedSearches;

namespace Imova.Application.Features.SavedSearches;

public static class SavedSearchRules
{
    public const int MaxPerUser = 20;
}

// Running a saved search: its stored query string through the same parser the search endpoint
// uses (SearchQueryString), always newest first.
public static class SavedSearchMatches
{
    // Null when the stored query no longer parses (e.g. a filter was renamed since it was saved) —
    // such a search just has no matches until the user saves it again.
    public static SearchListingsQuery? ToQuery(SavedSearch savedSearch)
    {
        try
        {
            return SearchQueryString.Parse(savedSearch.QueryString) with { Sort = ListingSort.Newest, Page = 1 };
        }
        catch (ValidationException)
        {
            return null;
        }
    }

    // Matches published since the user last opened this search.
    public static async Task<int> CountNewAsync(IListingSearch listingSearch, SavedSearch savedSearch, CancellationToken cancellationToken)
    {
        if (ToQuery(savedSearch) is not { } query)
        {
            return 0;
        }

        var (_, total) = await listingSearch.SearchAsync(query with { PublishedAfter = savedSearch.LastViewedAt, PageSize = 1 }, cancellationToken);
        return total;
    }

    public static SavedSearchDto ToDto(this SavedSearch savedSearch, int newListingsCount) =>
        new(
            savedSearch.Id,
            savedSearch.Name,
            savedSearch.QueryString,
            savedSearch.AlertFrequency.ToString(),
            newListingsCount,
            savedSearch.CreatedAt);
}
