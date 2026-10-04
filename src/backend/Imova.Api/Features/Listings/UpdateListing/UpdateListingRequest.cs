using System.Text.Json;
using System.Text.Json.Serialization;
using Imova.Domain.Listings;
using Imova.Domain.Properties;

namespace Imova.Api.Features.Listings.UpdateListing;

// AgencyId must be sent, even when it's null (a private listing): leaving it out is a 400, never
// "move it out of its agency".
public record UpdateListingRequest(
    [property: JsonRequired] Guid? AgencyId,
    PropertyType PropertyType,
    decimal TotalAreaM2,
    int? YearBuilt,
    PropertyCondition? Condition,
    JsonElement? TypeSpecificAttributes,
    IReadOnlyList<Guid>? AmenityIds,
    IReadOnlyList<Guid>? ProximityIds,
    string Country,
    Guid RaionId,
    Guid? LocalitateId,
    Guid? ChisinauSectorId,
    string? StreetAddress,
    string? BuildingNumber,
    TransactionType TransactionType,
    string Title,
    string Description,
    decimal Price,
    Currency Currency,
    bool IsNegotiable,
    RentalDetails? RentalDetails,
    ListingContact? Contact);
