using Imova.Contracts.Common;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;
using MediatR;

namespace Imova.Application.Features.Listings.SearchListings;

public enum ListingSort
{
    Newest = 0,
    PriceAsc = 1,
    PriceDesc = 2,
    AreaDesc = 3,
}

// The /cauta results page: Active listings only, every filter optional, combined with AND.
// Multi-value filters (PropertyTypes and the enum filters) match any of their values; AmenityIds
// and ProximityIds require *all* of theirs. Prices are EUR (compared to Price.PriceEur), areas m².
// Type-specific filters need exactly one PropertyType they belong to (see SearchFilterRules);
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

    // --- Type-specific (TypeSpecificAttributes) ---
    public int? MinRooms { get; init; }

    public int? MaxRooms { get; init; }

    public int? MinFloor { get; init; }

    public int? MaxFloor { get; init; }

    public int? MinBathrooms { get; init; }

    public decimal? MinLandAreaM2 { get; init; }

    public decimal? MaxLandAreaM2 { get; init; }

    public IReadOnlyList<HousingStockType> HousingStockTypes { get; init; } = [];

    public IReadOnlyList<ApartmentLayout> Layouts { get; init; } = [];

    public IReadOnlyList<HeatingSystem> HeatingSystems { get; init; } = [];

    public IReadOnlyList<HouseType> HouseTypes { get; init; } = [];

    public IReadOnlyList<PlotType> PlotTypes { get; init; } = [];

    public IReadOnlyList<LocationContext> LocationContexts { get; init; } = [];

    public IReadOnlyList<RoadAccess> RoadAccesses { get; init; } = [];

    public IReadOnlyList<CommercialSpaceType> SpaceTypes { get; init; } = [];

    public IReadOnlyList<ParkingType> ParkingTypes { get; init; } = [];

    public IReadOnlyList<BathroomType> BathroomTypes { get; init; } = [];

    // --- Rental terms (RentalDetails) ---
    public bool? PetsAllowed { get; init; }

    public bool? UtilitiesIncluded { get; init; }

    public int? MaxLeasePeriodMonths { get; init; }

    public ListingSort Sort { get; init; } = ListingSort.Newest;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = SearchFilterRules.DefaultPageSize;

    public Guid? CurrentUserId { get; init; }
}
