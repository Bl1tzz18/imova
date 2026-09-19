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
        double latitude,
        double longitude)
        : base(id)
    {
        PropertyId = propertyId;
        Country = country;
        City = city;
        District = district;
        Latitude = latitude;
        Longitude = longitude;

        Location = new Point(longitude, latitude)
        {
            SRID = 4326
        };
    }

    public Guid PropertyId { get; private set; }

    public string Country { get; private set; }

    public string? Region { get; private set; }

    public string City { get; private set; }

    public string? District { get; private set; }

    public string? Sector { get; private set; }

    public string? Street { get; private set; }

    public string? BuildingNumber { get; private set; }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public Point Location { get; private set; }

    public static PropertyLocation Create(
        Guid propertyId,
        string country,
        string city,
        string? district,
        double latitude,
        double longitude)
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
            longitude);
    }

    public void UpdateDetails(string country, string city, string? district, double latitude, double longitude)
    {
        EnsureValidDetails(country, city, latitude, longitude);

        Country = country;
        City = city;
        District = district;
        Latitude = latitude;
        Longitude = longitude;
        Location = new Point(longitude, latitude)
        {
            SRID = 4326
        };
    }

    private static void EnsureValidDetails(string country, string city, double latitude, double longitude)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("Country is required.", nameof(country));
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ArgumentException("City is required.", nameof(city));
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
