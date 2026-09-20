using Imova.Domain.Common;
using NetTopologySuite.Geometries;

namespace Imova.Domain.Locations;

public sealed class PropertyLocation : Entity
{
    private PropertyLocation(
        Guid id,
        Guid propertyId,
        string country,
        Guid raionId,
        string raionName,
        Guid? localitateId,
        string? localitateName,
        Guid? chisinauSectorId,
        string? chisinauSectorName,
        double? latitude,
        double? longitude,
        string? street)
        : base(id)
    {
        PropertyId = propertyId;
        Country = country;
        RaionId = raionId;
        RaionName = raionName;
        LocalitateId = localitateId;
        LocalitateName = localitateName;
        ChisinauSectorId = chisinauSectorId;
        ChisinauSectorName = chisinauSectorName;
        Street = street;
        Latitude = latitude;
        Longitude = longitude;

        Location = BuildPoint(latitude, longitude);
    }

    public Guid PropertyId { get; private set; }

    public string Country { get; private set; }

    public string? Region { get; private set; }

    public Guid RaionId { get; private set; }

    // Denormalized snapshot of Raion.NameRo at write time — every property-returning handler
    // (17+ call sites through PropertyMapping.ToDto) reads location fields with zero extra DB
    // calls today; resolving RaionId/LocalitateId to a name at every read would mean batch-joining
    // Raion/Localitate in each of those handlers instead. CUATM administrative names are static,
    // so staleness risk is negligible.
    public string RaionName { get; private set; }

    public Guid? LocalitateId { get; private set; }

    // Always both-or-neither with LocalitateId — see EnsureValidDetails.
    public string? LocalitateName { get; private set; }

    // Informal Chișinău neighborhood (see ChisinauSector) — mutually exclusive with LocalitateId:
    // a listing can be in a suburb (Localitate) or an informal neighborhood (this), never both
    // (they're physically different places), though neither is fine. Always both-or-neither with
    // ChisinauSectorName, and never both-with-LocalitateId — see EnsureValidDetails. Not to be
    // confused with the dead Sector string column below.
    public Guid? ChisinauSectorId { get; private set; }

    public string? ChisinauSectorName { get; private set; }

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
        Guid raionId,
        string raionName,
        Guid? localitateId,
        string? localitateName,
        Guid? chisinauSectorId,
        string? chisinauSectorName,
        double? latitude,
        double? longitude,
        string? street = null)
    {
        if (propertyId == Guid.Empty)
        {
            throw new ArgumentException("PropertyId is required.", nameof(propertyId));
        }

        EnsureValidDetails(country, raionId, raionName, localitateId, localitateName, chisinauSectorId, chisinauSectorName, latitude, longitude);

        return new PropertyLocation(
            Guid.NewGuid(),
            propertyId,
            country,
            raionId,
            raionName,
            localitateId,
            localitateName,
            chisinauSectorId,
            chisinauSectorName,
            latitude,
            longitude,
            street);
    }

    public void UpdateDetails(
        string country,
        Guid raionId,
        string raionName,
        Guid? localitateId,
        string? localitateName,
        Guid? chisinauSectorId,
        string? chisinauSectorName,
        double? latitude,
        double? longitude,
        string? street = null)
    {
        EnsureValidDetails(country, raionId, raionName, localitateId, localitateName, chisinauSectorId, chisinauSectorName, latitude, longitude);

        Country = country;
        RaionId = raionId;
        RaionName = raionName;
        LocalitateId = localitateId;
        LocalitateName = localitateName;
        ChisinauSectorId = chisinauSectorId;
        ChisinauSectorName = chisinauSectorName;
        Street = street;
        Latitude = latitude;
        Longitude = longitude;
        Location = BuildPoint(latitude, longitude);
    }

    private static Point? BuildPoint(double? latitude, double? longitude) =>
        latitude.HasValue && longitude.HasValue
            ? new Point(longitude.Value, latitude.Value) { SRID = 4326 }
            : null;

    private static void EnsureValidDetails(
        string country,
        Guid raionId,
        string raionName,
        Guid? localitateId,
        string? localitateName,
        Guid? chisinauSectorId,
        string? chisinauSectorName,
        double? latitude,
        double? longitude)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("Country is required.", nameof(country));
        }

        if (raionId == Guid.Empty)
        {
            throw new ArgumentException("RaionId is required.", nameof(raionId));
        }

        if (string.IsNullOrWhiteSpace(raionName))
        {
            throw new ArgumentException("RaionName is required.", nameof(raionName));
        }

        if (localitateId.HasValue != !string.IsNullOrWhiteSpace(localitateName))
        {
            throw new ArgumentException("LocalitateId and LocalitateName must both be set or both be empty.");
        }

        if (chisinauSectorId.HasValue != !string.IsNullOrWhiteSpace(chisinauSectorName))
        {
            throw new ArgumentException("ChisinauSectorId and ChisinauSectorName must both be set or both be empty.");
        }

        // A listing can be in a suburb (LocalitateId) or an informal Chișinău neighborhood
        // (ChisinauSectorId), never both at once — those represent physically different places.
        // Enforced here too (not just in CreatePropertyValidator/UpdatePropertyValidator) so the
        // entity can't be put into a contradictory state by any other caller.
        if (localitateId.HasValue && chisinauSectorId.HasValue)
        {
            throw new ArgumentException("LocalitateId and ChisinauSectorId cannot both be set.");
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
