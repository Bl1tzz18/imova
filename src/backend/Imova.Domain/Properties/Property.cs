using Imova.Domain.Common;
using Imova.Domain.Properties.Attributes;

namespace Imova.Domain.Properties;

// The physical real-estate asset — the same data regardless of who is selling or renting it, or
// how. Everything about the *offer* (title, price, status/moderation, who published it) lives on
// Listing instead, so one Property can carry several Listings over its life (sold, then relisted;
// a sale listing and a separate rental listing; ...).
public sealed class Property : AggregateRoot
{
    private readonly List<PropertyAmenity> _amenities = [];
    private readonly List<PropertyProximity> _proximities = [];

    // For EF Core materialization only.
    private Property()
        : base(Guid.Empty)
    {
        TypeSpecificAttributes = null!;
    }

    private Property(
        Guid id,
        PropertyType propertyType,
        decimal totalAreaM2,
        int? yearBuilt,
        PropertyCondition? condition,
        Guid locationId,
        PropertyAttributes typeSpecificAttributes)
        : base(id)
    {
        PropertyType = propertyType;
        TotalAreaM2 = totalAreaM2;
        YearBuilt = yearBuilt;
        Condition = condition;
        LocationId = locationId;
        TypeSpecificAttributes = typeSpecificAttributes;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public PropertyType PropertyType { get; private set; }

    // Always square meters, including for Land — the UI may display/convert to "ari", but storage
    // stays m² so area filtering/sorting compares like with like.
    public decimal TotalAreaM2 { get; private set; }

    // Not applicable to Land — see EnsureValidDetails.
    public int? YearBuilt { get; private set; }

    // Nullable: not meaningful for Land, and listings created before this field existed have no
    // value for it.
    public PropertyCondition? Condition { get; private set; }

    public Guid LocationId { get; private set; }

    // Stored as JSONB; which concrete record this is always matches PropertyType (enforced in
    // EnsureValidDetails). See PropertyAttributes for the per-type schemas.
    public PropertyAttributes TypeSpecificAttributes { get; private set; }

    public IReadOnlyCollection<PropertyAmenity> Amenities => _amenities.AsReadOnly();

    // What the property is close to (school, park, ...) — see Proximity.
    public IReadOnlyCollection<PropertyProximity> Proximities => _proximities.AsReadOnly();

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Property Create(
        PropertyType propertyType,
        decimal totalAreaM2,
        int? yearBuilt,
        PropertyCondition? condition,
        Guid locationId,
        PropertyAttributes typeSpecificAttributes,
        IEnumerable<Guid>? amenityIds = null,
        IEnumerable<Guid>? proximityIds = null,
        Guid? id = null)
    {
        if (locationId == Guid.Empty)
        {
            throw new ArgumentException("LocationId is required.", nameof(locationId));
        }

        EnsureValidDetails(propertyType, totalAreaM2, yearBuilt, condition, typeSpecificAttributes);

        var property = new Property(
            id ?? Guid.NewGuid(), propertyType, totalAreaM2, yearBuilt, condition, locationId, typeSpecificAttributes);
        property.ReplaceAmenities(amenityIds ?? []);
        property.ReplaceProximities(proximityIds ?? []);
        return property;
    }

    // LocationId is deliberately not editable — the location row itself is updated in place
    // (PropertyLocation.UpdateDetails) rather than swapped for another one.
    public void UpdateDetails(
        PropertyType propertyType,
        decimal totalAreaM2,
        int? yearBuilt,
        PropertyCondition? condition,
        PropertyAttributes typeSpecificAttributes,
        IEnumerable<Guid> amenityIds,
        IEnumerable<Guid> proximityIds)
    {
        EnsureValidDetails(propertyType, totalAreaM2, yearBuilt, condition, typeSpecificAttributes);

        PropertyType = propertyType;
        TotalAreaM2 = totalAreaM2;
        YearBuilt = yearBuilt;
        Condition = condition;
        TypeSpecificAttributes = typeSpecificAttributes;
        ReplaceAmenities(amenityIds);
        ReplaceProximities(proximityIds);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void ReplaceAmenities(IEnumerable<Guid> amenityIds)
    {
        var wanted = amenityIds.Where(a => a != Guid.Empty).ToHashSet();

        _amenities.RemoveAll(a => !wanted.Contains(a.AmenityId));
        foreach (var amenityId in wanted.Where(a => _amenities.All(existing => existing.AmenityId != a)))
        {
            _amenities.Add(PropertyAmenity.Create(Id, amenityId));
        }
    }

    private void ReplaceProximities(IEnumerable<Guid> proximityIds)
    {
        var wanted = proximityIds.Where(p => p != Guid.Empty).ToHashSet();

        _proximities.RemoveAll(p => !wanted.Contains(p.ProximityId));
        foreach (var proximityId in wanted.Where(p => _proximities.All(existing => existing.ProximityId != p)))
        {
            _proximities.Add(PropertyProximity.Create(Id, proximityId));
        }
    }

    private static void EnsureValidDetails(
        PropertyType propertyType,
        decimal totalAreaM2,
        int? yearBuilt,
        PropertyCondition? condition,
        PropertyAttributes typeSpecificAttributes)
    {
        if (totalAreaM2 <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalAreaM2), "TotalAreaM2 must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(typeSpecificAttributes);

        if (typeSpecificAttributes.GetPropertyType() != propertyType)
        {
            throw new ArgumentException(
                $"{typeSpecificAttributes.GetType().Name} does not apply to a {propertyType} property.",
                nameof(typeSpecificAttributes));
        }

        if (propertyType == PropertyType.Land && yearBuilt.HasValue)
        {
            throw new ArgumentException("YearBuilt does not apply to Land.", nameof(yearBuilt));
        }

        if (propertyType == PropertyType.Land && condition.HasValue)
        {
            throw new ArgumentException("Condition does not apply to Land.", nameof(condition));
        }
    }
}
