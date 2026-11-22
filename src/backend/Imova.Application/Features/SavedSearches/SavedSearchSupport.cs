using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Validation;
using Imova.Application.Features.Listings.SearchListings;
using Imova.Contracts.SavedSearches;
using Imova.Domain.SavedSearches;

namespace Imova.Application.Features.SavedSearches;

public static class SavedSearchRules
{
    public const int MaxPerUser = 20;

    // The channels a request asks for: what it says, else what the search already had. A search
    // needs at least one (no alerts at all is AlertFrequency.Off) — a coded 400 otherwise.
    public static AlertChannels Channels(bool? byEmail, bool? byTelegram, AlertChannels current)
    {
        var channels = AlertChannels.None;
        if (byEmail ?? current.HasFlag(AlertChannels.Email)) channels |= AlertChannels.Email;
        if (byTelegram ?? current.HasFlag(AlertChannels.Telegram)) channels |= AlertChannels.Telegram;
        if (channels == AlertChannels.None)
        {
            throw new ValidationException(
                [CodedFailure.Of("AlertByEmail", "Alerts must go by email, Telegram or both.", ErrorCodes.SavedSearchNoChannel)]);
        }

        return channels;
    }
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
            savedSearch.CreatedAt,
            savedSearch.AlertChannels.HasFlag(AlertChannels.Email),
            savedSearch.AlertChannels.HasFlag(AlertChannels.Telegram));
}
