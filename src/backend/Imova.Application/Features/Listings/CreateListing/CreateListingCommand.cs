using System.Text.Json;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using MediatR;

namespace Imova.Application.Features.Listings.CreateListing;

// Everything the listing form collects, in one payload: the physical Property (type, area,
// attributes, amenities, address) and the Listing offer on top of it. Both are created together,
// atomically — see CreateListingHandler.
//
// RequestingUserId always comes from the caller's JWT (see CreateListingEndpoint), never the body.
// PublisherId picks which of the caller's publishers (Individual/Agency) this is published under;
// omitted means their Individual publisher.
//
// Id is client-supplied and optional: the form generates one up front so it can attach uploaded
// photos (ConfirmMediaUpload) to that listing id before the listing row exists.
public record CreateListingCommand(
    Guid? Id,
    Guid RequestingUserId,
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
    RentalDetails? RentalDetails) : IRequest<ListingDto>, IListingWriteCommand;
