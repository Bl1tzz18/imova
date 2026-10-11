using Imova.Application.Features.Listings;
using Imova.Application.Features.Listings.Expiry;
using Imova.Application.Features.Locations.SearchLocations;
using Imova.Domain.Listings;

namespace Imova.Application.Features.Agencies.Listings;

public enum AgencyListingGroup
{
    Active,
    Unpublished,
    Ended,
}

public enum AgencyListingSort
{
    Recommended,
    Newest,
    PriceAsc,
    PriceDesc,
}

// What the management list needs of a listing to group, search and order it — cheap to load for
// every listing of the agency; the full ListingDto is only built for the page shown.
public sealed record AgencyListingRow(
    Guid Id,
    string Title,
    ListingStatus Status,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    decimal PriceEur,
    string? RaionName,
    string? LocalitateName,
    string? ChisinauSectorName,
    string? Street,
    int PhotoCount);

// The agency's listings in the same three tabs, search and orders as "Anunțurile mele" — the same
// rules as the web app's lib/listing/ownerGroups.ts, applied on the server so the list can be paged.
public static class AgencyListingGroups
{
    public static AgencyListingGroup GroupOf(ListingStatus status) => status switch
    {
        ListingStatus.Active => AgencyListingGroup.Active,
        ListingStatus.Draft or ListingStatus.PendingReview or ListingStatus.Rejected or ListingStatus.Suspended
            => AgencyListingGroup.Unpublished,
        _ => AgencyListingGroup.Ended, // Expired, Archived, Sold, Rented
    };

    // Waiting on the owner: fix a rejected/suspended listing, submit a draft, renew before it expires,
    // add the photos an older listing is short of (ListingPhotoRules; sold and rented are over).
    public static bool NeedsAttention(AgencyListingRow row, DateTimeOffset now) =>
        row.Status is ListingStatus.Rejected or ListingStatus.Suspended or ListingStatus.Draft
        || (row.Status == ListingStatus.Active && row.ExpiresAt is { } expiresAt && expiresAt - now <= ListingExpiry.ReminderBefore)
        || (row.Status is not (ListingStatus.Sold or ListingStatus.Rented) && row.PhotoCount < ListingPhotoRules.MinPhotos);

    // Every word in the title or the place (raion, locality, neighborhood, street), ignoring case and
    // diacritics.
    public static bool Matches(AgencyListingRow row, string? query)
    {
        var words = LocationSearchText.Normalize(query ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return true;
        }

        var haystack = LocationSearchText.Normalize(string.Join(
            ' ',
            new[] { row.Title, row.RaionName, row.LocalitateName, row.ChisinauSectorName, row.Street }.Where(s => !string.IsNullOrWhiteSpace(s))));
        return words.All(haystack.Contains);
    }

    // Recommended: what needs the owner first, then the most recently changed. Id breaks ties so pages
    // never overlap.
    public static IEnumerable<AgencyListingRow> Order(IEnumerable<AgencyListingRow> rows, AgencyListingSort sort, DateTimeOffset now) =>
        sort switch
        {
            AgencyListingSort.Newest => rows.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id),
            AgencyListingSort.PriceAsc => rows.OrderBy(r => r.PriceEur).ThenBy(r => r.Id),
            AgencyListingSort.PriceDesc => rows.OrderByDescending(r => r.PriceEur).ThenBy(r => r.Id),
            _ => rows.OrderByDescending(r => NeedsAttention(r, now)).ThenByDescending(r => r.UpdatedAt).ThenBy(r => r.Id),
        };

    // The tab to show: the one asked for, else Active, else the first with anything in it.
    public static AgencyListingGroup InitialGroup(AgencyListingGroup? requested, IReadOnlyDictionary<AgencyListingGroup, int> counts)
    {
        if (requested is { } group)
        {
            return group;
        }

        if (counts[AgencyListingGroup.Active] > 0)
        {
            return AgencyListingGroup.Active;
        }

        return Enum.GetValues<AgencyListingGroup>().FirstOrDefault(g => counts[g] > 0, AgencyListingGroup.Active);
    }

    public static AgencyListingGroup? ParseGroup(string? value) =>
        Enum.TryParse<AgencyListingGroup>(value, ignoreCase: true, out var group) && Enum.IsDefined(group) ? group : null;

    public static AgencyListingSort ParseSort(string? value) =>
        Enum.TryParse<AgencyListingSort>(value, ignoreCase: true, out var sort) && Enum.IsDefined(sort) ? sort : AgencyListingSort.Recommended;
}
