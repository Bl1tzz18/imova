using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;

namespace Imova.Application.Features.Listings.GetSimilarListings;

// What a similar listing is compared against.
// Area: the most precise place the listing names — its Chișinău neighborhood (sector) or its
// locality — or neither (only a raion). City: the raion (Chișinău counts as one). Rooms: only for an
// Apartment or a House.
public sealed record SimilarListingTarget(
    Guid ListingId,
    TransactionType TransactionType,
    PropertyType PropertyType,
    Guid RaionId,
    Guid? ChisinauSectorId,
    Guid? LocalitateId,
    decimal PriceEur,
    int? Rooms)
{
    public bool HasArea => ChisinauSectorId is not null || LocalitateId is not null;

    public static SimilarListingTarget For(Listing listing, Property property, Guid raionId, Guid? sectorId, Guid? localitateId) =>
        new(
            listing.Id,
            listing.TransactionType,
            property.PropertyType,
            raionId,
            sectorId,
            localitateId,
            listing.Price.PriceEur,
            property.TypeSpecificAttributes switch
            {
                ApartmentAttributes a => a.Rooms,
                HouseAttributes h => h.Rooms,
                _ => null,
            });
}

// One attempt's WHERE on top of the hard filters (same transaction + property type, Active, not the
// listing itself, which never change): only the same area, only the same city, and/or a price
// within ±PriceTolerance of the listing's. The ORDER BY is the same at every stage — same area, then
// same city, then closer price, then a room count within one, then the newest.
public sealed record SimilarListingStage(bool SameAreaOnly, bool SameCityOnly, decimal? PriceTolerance);

public static class SimilarListingStages
{
    // Enough to show the section without broadening further; never more than MaxResults.
    public const int MinResults = 4;
    public const int MaxResults = 6;

    // From the tightest to national, each one a superset of the one before:
    // 1. the same area (when the listing names one — else the same city) and price ±20%;
    // 2. the same city, price ±30%; 3. the same city, any price; 4. anywhere in the country.
    // Without a price to compare (0), the price stages are skipped.
    public static IReadOnlyList<SimilarListingStage> For(SimilarListingTarget target)
    {
        var withPrice = target.PriceEur > 0;
        var stages = new List<SimilarListingStage>();
        if (withPrice)
        {
            stages.Add(target.HasArea
                ? new SimilarListingStage(SameAreaOnly: true, SameCityOnly: true, PriceTolerance: 0.20m)
                : new SimilarListingStage(SameAreaOnly: false, SameCityOnly: true, PriceTolerance: 0.20m));
            stages.Add(new SimilarListingStage(SameAreaOnly: false, SameCityOnly: true, PriceTolerance: 0.30m));
        }

        stages.Add(new SimilarListingStage(SameAreaOnly: false, SameCityOnly: true, PriceTolerance: null));
        stages.Add(new SimilarListingStage(SameAreaOnly: false, SameCityOnly: false, PriceTolerance: null));
        return stages;
    }
}
