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
    // Free-text, optional — fed into IGeocodingService alongside Localitate/Raion/Country for
    // building-level precision (see PropertyAddress.Compose).
    string? StreetAddress,
    decimal? Area,
    decimal? Rooms,
    short? Bathrooms,
    short? Floor,
    short? TotalFloors,
    short? YearBuilt,
    bool? Furnished,
    bool? ParkingAvailable,
    bool? PetsAllowed) : IRequest<PropertyDto>;
