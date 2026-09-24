namespace Imova.Contracts.Amenities;

// Category groups amenities for display: General, Comfort, Security or Leisure.
// ApplicablePropertyTypes lists the PropertyType names the amenity can be selected for.
public record AmenityDto(Guid Id, string Key, string LabelRo, string Category, IReadOnlyList<string> ApplicablePropertyTypes);
