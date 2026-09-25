namespace Imova.Domain.Properties;

// Join row between Property and the seeded Proximity reference table. Lives on Property, like
// PropertyAmenity — what the property is close to doesn't change when it's relisted.
public sealed class PropertyProximity
{
    private PropertyProximity(Guid propertyId, Guid proximityId)
    {
        PropertyId = propertyId;
        ProximityId = proximityId;
    }

    public Guid PropertyId { get; private set; }

    public Guid ProximityId { get; private set; }

    internal static PropertyProximity Create(Guid propertyId, Guid proximityId) => new(propertyId, proximityId);
}
