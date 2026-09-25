using Imova.Domain.Properties;

namespace Imova.Application.Features.Listings.SearchListings;

// Which property types each type-specific filter belongs to — it only means something for those
// (rooms don't exist on a plot of land), so it's accepted only when the search is for exactly one
// of them. Mirrored by the frontend's lib/search/filters.ts.
public static class SearchFilterRules
{
    public const int DefaultPageSize = 24;
    public const int MaxPageSize = 60;

    public static readonly IReadOnlyDictionary<string, PropertyType[]> TypeSpecificFilters =
        new Dictionary<string, PropertyType[]>
        {
            ["rooms"] = [PropertyType.Apartment, PropertyType.House],
            ["floor"] = [PropertyType.Apartment, PropertyType.Commercial],
            ["bathrooms"] = [PropertyType.Apartment],
            ["landAreaM2"] = [PropertyType.House],
            ["housingStockType"] = [PropertyType.Apartment],
            ["layout"] = [PropertyType.Apartment],
            ["heatingSystem"] = [PropertyType.Apartment, PropertyType.House],
            ["houseType"] = [PropertyType.House],
            ["plotType"] = [PropertyType.Land],
            ["locationContext"] = [PropertyType.Land],
            ["roadAccess"] = [PropertyType.Land],
            ["spaceType"] = [PropertyType.Commercial],
            ["parkingType"] = [PropertyType.Garage],
            ["bathroomType"] = [PropertyType.Room],
        };

    // The type-specific filters a query actually uses.
    public static IEnumerable<string> UsedTypeSpecificFilters(SearchListingsQuery q)
    {
        if (q.MinRooms is not null || q.MaxRooms is not null) yield return "rooms";
        if (q.MinFloor is not null || q.MaxFloor is not null) yield return "floor";
        if (q.MinBathrooms is not null) yield return "bathrooms";
        if (q.MinLandAreaM2 is not null || q.MaxLandAreaM2 is not null) yield return "landAreaM2";
        if (q.HousingStockTypes.Count > 0) yield return "housingStockType";
        if (q.Layouts.Count > 0) yield return "layout";
        if (q.HeatingSystems.Count > 0) yield return "heatingSystem";
        if (q.HouseTypes.Count > 0) yield return "houseType";
        if (q.PlotTypes.Count > 0) yield return "plotType";
        if (q.LocationContexts.Count > 0) yield return "locationContext";
        if (q.RoadAccesses.Count > 0) yield return "roadAccess";
        if (q.SpaceTypes.Count > 0) yield return "spaceType";
        if (q.ParkingTypes.Count > 0) yield return "parkingType";
        if (q.BathroomTypes.Count > 0) yield return "bathroomType";
    }

    public static bool UsesRentalFilters(SearchListingsQuery q) =>
        q.PetsAllowed is not null || q.UtilitiesIncluded is not null || q.MaxLeasePeriodMonths is not null;
}
