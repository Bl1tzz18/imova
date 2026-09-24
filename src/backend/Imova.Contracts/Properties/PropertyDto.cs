using System.Text.Json;
using Imova.Contracts.Amenities;

namespace Imova.Contracts.Properties;

// The physical asset behind a listing. TypeSpecificAttributes is the per-PropertyType object
// (camelCase keys, enums as strings) — its exact shape depends on PropertyType; see
// Imova.Domain.Properties.Attributes for the schemas.
public record PropertyDto(
    Guid Id,
    string PropertyType,
    decimal TotalAreaM2,
    int? YearBuilt,
    string? Condition,
    JsonElement TypeSpecificAttributes,
    IReadOnlyList<AmenityDto> Amenities,
    PropertyLocationDto? Location);
