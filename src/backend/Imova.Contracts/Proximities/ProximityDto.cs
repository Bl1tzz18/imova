namespace Imova.Contracts.Proximities;

// ApplicablePropertyTypes lists the PropertyType names the proximity can be selected for.
public record ProximityDto(Guid Id, string Key, string LabelRo, IReadOnlyList<string> ApplicablePropertyTypes);
