using System.Text.Json;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using MediatR;

namespace Imova.Application.Features.Listings.UpdateListing;

// RequestingUserId/IsAdmin always come from the caller's JWT claims (see UpdateListingEndpoint).
//
// Same field set as CreateListingCommand (minus Id/PublisherId, which never change on edit) —
// the edit page reuses the exact same multi-step form as creation, with every field pre-filled
// and editable. Physical fields update the listing's Property; offer fields update the Listing.
public record UpdateListingCommand(
    Guid Id,
    Guid RequestingUserId,
    bool IsAdmin,
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
    ListingContact? Contact) : IRequest<ListingDto?>, IListingWriteCommand;
