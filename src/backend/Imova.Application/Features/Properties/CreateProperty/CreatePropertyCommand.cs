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
//
// No Latitude/Longitude here on purpose — CreatePropertyHandler derives coordinates server-side
// via IGeocodingService from Country/Raion/Localitate, rather than trusting client-supplied
// coordinates for a listing's real-world location (see PropertyLocation, which stores them as
// nullable: geocoding failing doesn't block the listing from being created).
//
// RaionId/LocalitateId reference the CUATM-seeded Raioane/Localitati reference tables (see
// CreatePropertyValidator's MustAsync existence checks) — replaces free-text City/District for
// data quality/geocoding reliability. LocalitateId is optional — a Raion alone is enough to
// geocode and save.
//
// ChisinauSectorId references the ChisinauSectors reference table (informal neighborhood names,
// e.g. "Botanica" — not CUATM data, see ChisinauSector.cs) — only meaningful when RaionId is
// Chișinău's, enforced by CreatePropertyValidator. Independent of LocalitateId: both, either, or
// neither may be set.
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
    Guid RaionId,
    Guid? LocalitateId,
    Guid? ChisinauSectorId,
    // Required (see CreatePropertyValidator's NotEmpty rule) — fed into IGeocodingService
    // alongside Localitate/Raion/Country for building-level precision (see
    // PropertyAddress.Compose). Still nullable at the type level since a malformed request could
    // still send null; validation, not the C# type, is what actually enforces this.
    string? StreetAddress,
    // Free-text, optional — the building/door number, e.g. "44" or "44A". Folded into the same
    // geocoding token as StreetAddress, right after it (see PropertyAddress.Compose), rather than
    // kept as a separate address-query part.
    string? BuildingNumber,
    decimal? Area,
    decimal? Rooms,
    short? Bathrooms,
    short? Floor,
    short? TotalFloors,
    short? YearBuilt,
    bool? Furnished,
    bool? ParkingAvailable,
    bool? PetsAllowed) : IRequest<PropertyDto>;
