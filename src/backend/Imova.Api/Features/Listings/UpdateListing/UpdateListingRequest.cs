using System.Text.Json;
using Imova.Domain.Listings;
using Imova.Domain.Properties;

namespace Imova.Api.Features.Listings.UpdateListing;

public record UpdateListingRequest(
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
