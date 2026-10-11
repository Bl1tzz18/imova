using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Domain.Agencies;

namespace Imova.Application.Features.Agencies.Import;

public enum ImportCoordinatesDecision
{
    Use = 1,
    OutsideMoldova = 2,
    // More than MaxDistanceKm from the row's locality / sector / city.
    TooFar = 3,
    // The locality couldn't be found on the map, so the coordinates can't be checked.
    Unverifiable = 4,
}

// Coordinates given in a CSV row are used only when they're plausibly that listing's: inside Moldova
// and within MaxDistanceKm of the place the row names (its sector, else its locality, else its city —
// found with the geocoder). Otherwise they're ignored with a warning and the address is geocoded, as
// for a listing made in the form.
public static class ImportCoordinates
{
    public const double MaxDistanceKm = 10;

    // Moldova's bounding box, with a small margin.
    private const double MinLatitude = 45.40;
    private const double MaxLatitude = 48.55;
    private const double MinLongitude = 26.55;
    private const double MaxLongitude = 30.20;

    public static bool InsideMoldova(double latitude, double longitude) =>
        latitude is >= MinLatitude and <= MaxLatitude && longitude is >= MinLongitude and <= MaxLongitude;

    // Great-circle distance (haversine), in kilometres.
    public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0088;
        static double Radians(double degrees) => degrees * Math.PI / 180;

        var dLat = Radians(lat2 - lat1);
        var dLon = Radians(lon2 - lon1);
        var a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(Radians(lat1)) * Math.Cos(Radians(lat2)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * earthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    public static ImportCoordinatesDecision Decide(double latitude, double longitude, GeocodingResult? place)
    {
        if (!InsideMoldova(latitude, longitude))
        {
            return ImportCoordinatesDecision.OutsideMoldova;
        }

        if (place is null)
        {
            return ImportCoordinatesDecision.Unverifiable;
        }

        return DistanceKm(latitude, longitude, place.Latitude, place.Longitude) <= MaxDistanceKm
            ? ImportCoordinatesDecision.Use
            : ImportCoordinatesDecision.TooFar;
    }
}

// Where on the map an imported listing goes. One per import: the places rows name are geocoded once
// each (the geocoder allows one request a second), the full addresses once per row that needs it.
public sealed class ImportGeolocator(IGeocodingService geocodingService)
{
    private const string Country = "Moldova";

    private readonly Dictionary<string, GeocodingResult?> _places = new(StringComparer.Ordinal);

    public sealed record Result(double? Latitude, double? Longitude, ListingImportProblem? Warning);

    public async Task<Result> LocateAsync(
        ImportLocation location, string? street, string? buildingNumber, double? latitude, double? longitude, CancellationToken cancellationToken)
    {
        ListingImportProblem? warning = null;
        if (latitude is { } lat && longitude is { } lon)
        {
            var place = await PlaceAsync(location, cancellationToken);
            var decision = ImportCoordinates.Decide(lat, lon, place);
            if (decision == ImportCoordinatesDecision.Use)
            {
                return new Result(lat, lon, null);
            }

            warning = ImportProblems.Warning(
                ImportColumns.Latitude,
                ErrorCodes.ImportCoordinatesIgnored,
                $"The coordinates were ignored ({decision}); the address was looked up instead.",
                ("reason", decision.ToString()),
                ("km", ImportCoordinates.MaxDistanceKm.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        var address = PropertyAddress.Compose(
            street, buildingNumber, location.ChisinauSectorName, location.LocalitateName, location.RaionName, Country);
        var geocoded = await geocodingService.GeocodeAsync(address, cancellationToken);
        return new Result(geocoded?.Latitude, geocoded?.Longitude, warning);
    }

    // The row's sector, else locality, else city, on the map (cached for the whole import).
    private async Task<GeocodingResult?> PlaceAsync(ImportLocation location, CancellationToken cancellationToken)
    {
        var address = PropertyAddress.Compose(null, null, location.ChisinauSectorName, location.LocalitateName, location.RaionName, Country);
        if (!_places.TryGetValue(address, out var place))
        {
            place = await geocodingService.GeocodeAsync(address, cancellationToken);
            _places[address] = place;
        }

        return place;
    }
}
