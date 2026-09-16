using Imova.Contracts.Properties;
using Imova.Domain.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.CreateProperty;

// OwnerId is supplied by the caller for now — there is no auth yet, so there's no
// authenticated-user context to pull it from. Once auth exists, this should come from
// the caller's claims instead of the request body.
//
// Area/Rooms/Bathrooms/Floor/TotalFloors/YearBuilt/Furnished/ParkingAvailable/PetsAllowed
// are all optional here because which of them are required, optional, or not applicable
// depends on PropertyType (and, for PetsAllowed, ListingType) — see
// PropertyFieldRules and CreatePropertyValidator.
//
// Id is also client-supplied and optional: the add-listing form generates one up front so it
// can attach uploaded photos (ConfirmMediaUpload) to that id before the property row exists.
// Falls back to a server-generated id when omitted, same as before that flow existed.
public record CreatePropertyCommand(
    Guid? Id,
    Guid OwnerId,
    string Title,
    string Description,
    PropertyType PropertyType,
    ListingType ListingType,
    decimal Price,
    string Currency,
    string Country,
    string City,
    string? District,
    double Latitude,
    double Longitude,
    decimal? Area,
    decimal? Rooms,
    short? Bathrooms,
    short? Floor,
    short? TotalFloors,
    short? YearBuilt,
    bool? Furnished,
    bool? ParkingAvailable,
    bool? PetsAllowed) : IRequest<PropertyDto>;
