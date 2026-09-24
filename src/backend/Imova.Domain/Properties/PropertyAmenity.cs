namespace Imova.Domain.Properties;

// Join row between Property and the seeded Amenity reference table. Lives on Property (the
// physical asset), not Listing — a balcony doesn't disappear when the property is relisted.
public sealed class PropertyAmenity
{
    private PropertyAmenity(Guid propertyId, Guid amenityId)
    {
        PropertyId = propertyId;
        AmenityId = amenityId;
    }

    public Guid PropertyId { get; private set; }

    public Guid AmenityId { get; private set; }

    internal static PropertyAmenity Create(Guid propertyId, Guid amenityId) => new(propertyId, amenityId);
}
