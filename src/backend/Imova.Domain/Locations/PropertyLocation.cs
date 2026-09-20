using Imova.Domain.Common;
using NetTopologySuite.Geometries;

namespace Imova.Domain.Locations;

public sealed class PropertyLocation : Entity
{
    private PropertyLocation(
        Guid id,
        Guid propertyId,
        string country,
        string city,
        string? district,
        double? latitude,
        double? longitude,
        string? street)
        : base(id)
    {
        PropertyId = propertyId;
        Country = country;
        City = city;
        District = district;
        Street = street;
        Latitude = latitude;
        Longitude = longitude;

        Location = BuildPoint(latitude, longitude);
    }

    public Guid PropertyId { get; private set; }

    public string Country { get; private set; }

    public string? Region { get; private set; }

    public string City { get; private set; }

    public string? District { get; private set; }

    public string? Sector { get; private set; }

    public string? Street { get; private set; }

    public string? BuildingNumber { get; private set; }

    // Null when geocoding hasn't run yet, or ran and couldn't resolve the address — a listing is
    // never blocked on this, see IGeocodingService. Always both-or-neither: EnsureValidDetails
    // rejects a mismatched pair.
    public double? Latitude { get; private set; }

    public double? Longitude { get; private set; }

    public Point? Location { get; private set; }

    public static PropertyLocation Create(
        Guid propertyId,
        string country,
        string city,
        string? district,
        double? latitude,
        double? longitude,
        string? street = null)
    {
        if (propertyId == Guid.Empty)
        {
            throw new ArgumentException("PropertyId is required.", nameof(propertyId));
        }

        EnsureValidDetails(country, city, latitude, longitude);

        return new PropertyLocation(
            Guid.NewGuid(),
            propertyId,
            country,
            city,
            district,
            latitude,
            longitude,
            street);
    }

    public void UpdateDetails(
        string country,
        string city,
        string? district,
        double? latitude,
        double? longitude,
        string? street = null)
    {
        EnsureValidDetails(country, city, latitude, longitude);

        Country = country;
        City = city;
        District = district;
        Street = street;
        Latitude = latitude;
        Longitude = longitude;
        Location = BuildPoint(latitude, longitude);
    }

    private static Point? BuildPoint(double? latitude, double? longitude) =>
        latitude.HasValue && longitude.HasValue
            ? new Point(longitude.Value, latitude.Value) { SRID = 4326 }
            : null;

    private static void EnsureValidDetails(string country, string city, double? latitude, double? longitude)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("Country is required.", nameof(country));
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ArgumentException("City is required.", nameof(city));
        }

        if (latitude.HasValue != longitude.HasValue)
        {
            throw new ArgumentException("Latitude and Longitude must both be set or both be null.");
        }

        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }
    }
}
