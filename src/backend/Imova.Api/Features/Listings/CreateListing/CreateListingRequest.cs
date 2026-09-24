using System.Text.Json;
using Imova.Domain.Listings;
using Imova.Domain.Properties;

namespace Imova.Api.Features.Listings.CreateListing;

// Same shape as CreateListingCommand minus RequestingUserId — the caller's identity comes from
// their JWT (see CreateListingEndpoint), never from the request body.
public record CreateListingRequest(
    Guid? Id,
    Guid? PublisherId,
    PropertyType PropertyType,
    decimal TotalAreaM2,
    int? YearBuilt,
    PropertyCondition? Condition,
    JsonElement? TypeSpecificAttributes,
    IReadOnlyList<Guid>? AmenityIds,
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
    RentalDetails? RentalDetails);
