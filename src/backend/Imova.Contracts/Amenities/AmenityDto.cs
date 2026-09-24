namespace Imova.Contracts.Amenities;

// Category groups amenities for display: General, Comfort, Security or Leisure.
public record AmenityDto(Guid Id, string Key, string LabelRo, string Category);
