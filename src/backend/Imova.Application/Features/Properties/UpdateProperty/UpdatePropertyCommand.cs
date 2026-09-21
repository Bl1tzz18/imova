using Imova.Contracts.Properties;
using Imova.Domain.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.UpdateProperty;

// RequestingUserId/IsAdmin are always supplied by the endpoint from the caller's JWT claims —
// never trust these from the request body (see CreatePropertyCommand's OwnerId for the same rule).
//
// Same field set as CreatePropertyCommand (minus Id/OwnerId, which never change on edit) — the
// frontend edit page reuses the exact same multi-step form as listing creation (PropertyForm.tsx)
// with every field pre-filled and editable, so the command needs to carry all of them, not just
// title/description/price.
public record UpdatePropertyCommand(
    Guid Id,
    Guid RequestingUserId,
    bool IsAdmin,
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
    // Required (see UpdatePropertyValidator's NotEmpty rule) — see CreatePropertyCommand's
    // StreetAddress for why the C# type stays nullable.
    string? StreetAddress,
    // Free-text, optional — see CreatePropertyCommand's BuildingNumber.
    string? BuildingNumber,
    decimal? Area,
    decimal? Rooms,
    short? Bathrooms,
    short? Floor,
    short? TotalFloors,
    short? YearBuilt,
    bool? Furnished,
    bool? ParkingAvailable,
    bool? PetsAllowed) : IRequest<PropertyDto?>;
