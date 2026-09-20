using Imova.Domain.Properties;

namespace Imova.Api.Features.Properties.CreateProperty;

// Same shape as CreatePropertyCommand minus OwnerId — the caller's identity comes from their JWT
// (see CreatePropertyEndpoint), never from the request body.
public record CreatePropertyRequest(
    Guid? Id,
    string Title,
    string Description,
    PropertyType PropertyType,
    ListingType ListingType,
    decimal Price,
    string Currency,
    string Country,
    Guid RaionId,
    Guid? LocalitateId,
    Guid? ChisinauSectorId,
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
