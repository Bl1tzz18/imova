using Imova.Contracts.Common;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using MediatR;

namespace Imova.Application.Features.Listings.SearchListings;

public enum ListingSort
{
    Newest = 0,
    PriceAsc = 1,
    PriceDesc = 2,
    AreaDesc = 3,
    AreaAsc = 4,
}

// The /search results page: Active listings only, every filter optional, combined with AND.
// Multi-value filters (PropertyTypes and the enum filters) match any of their values; AmenityIds
// and ProximityIds require *all* of theirs. Prices are EUR (compared to Price.PriceEur), areas m².
// Attribute filters need exactly one PropertyType whose attributes have that field;
// rental filters only apply to rentals. MaxLeasePeriodMonths: the listing's required minimum lease
// is at most this long (or it has none).
public record SearchListingsQuery : IRequest<PagedResult<ListingDto>>
{
    public TransactionType? TransactionType { get; init; }

    public IReadOnlyList<PropertyType> PropertyTypes { get; init; } = [];

    public decimal? MinPriceEur { get; init; }

    public decimal? MaxPriceEur { get; init; }

    public Guid? RaionId { get; init; }

    public Guid? LocalitateId { get; init; }

    public Guid? ChisinauSectorId { get; init; }

    public decimal? MinAreaM2 { get; init; }

    public decimal? MaxAreaM2 { get; init; }

    public IReadOnlyList<Guid> AmenityIds { get; init; } = [];

    public IReadOnlyList<Guid> ProximityIds { get; init; } = [];

    // General Property fields the listing form asks per type: year built (not Land) and the
    // general condition (only Garage and Room use it).
    public int? MinYearBuilt { get; init; }

    public int? MaxYearBuilt { get; init; }

    public IReadOnlyList<PropertyCondition> Conditions { get; init; } = [];

    // --- Type-specific (TypeSpecificAttributes), see AttributeFilterParser ---
    public IReadOnlyList<AttributeFilter> AttributeFilters { get; init; } = [];

    // --- Rental terms (RentalDetails) ---
    public bool? PetsAllowed { get; init; }

    public bool? UtilitiesIncluded { get; init; }

    public int? MaxLeasePeriodMonths { get; init; }

    public ListingSort Sort { get; init; } = ListingSort.Newest;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = SearchFilterRules.DefaultPageSize;

    // The map view (GET /listings/search/map): only listings that have coordinates, and a page up
    // to SearchFilterRules.MaxMapResults so one response holds every pin.
    public bool OnlyWithCoordinates { get; init; }

    public Guid? CurrentUserId { get; init; }

    // Only listings published in (PublishedAfter, PublishedBefore] — saved searches' "new since your
    // last visit" and alerts. Set in code, never read from the query string.
    public DateTimeOffset? PublishedAfter { get; init; }

    public DateTimeOffset? PublishedBefore { get; init; }
}
