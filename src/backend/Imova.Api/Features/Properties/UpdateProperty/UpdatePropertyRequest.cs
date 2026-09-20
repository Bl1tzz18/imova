using Imova.Domain.Properties;

namespace Imova.Api.Features.Properties.UpdateProperty;

// Same shape as CreatePropertyRequest minus Id — the edit form reuses the exact same fields the
// create form does (see PropertyForm.tsx), just PUT against an existing listing's id from the route.
public record UpdatePropertyRequest(
    string Title,
    string Description,
    PropertyType PropertyType,
    ListingType ListingType,
    decimal Price,
    string Currency,
    string Country,
    Guid RaionId,
    Guid? LocalitateId,
    string? StreetAddress,
    decimal? Area,
    decimal? Rooms,
    short? Bathrooms,
    short? Floor,
    short? TotalFloors,
    short? YearBuilt,
    bool? Furnished,
    bool? ParkingAvailable,
    bool? PetsAllowed);
