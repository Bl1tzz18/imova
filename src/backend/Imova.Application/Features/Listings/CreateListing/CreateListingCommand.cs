using System.Text.Json;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using MediatR;

namespace Imova.Application.Features.Listings.CreateListing;

// Everything the listing form collects, in one payload: the physical Property (type, area,
// attributes, amenities, proximities, address) and the Listing offer on top of it. Both are created together,
// atomically — see CreateListingHandler.
//
// RequestingUserId always comes from the caller's JWT (see CreateListingEndpoint), never the body;
// the caller is the author (their own Publisher). AgencyId publishes it under one of the caller's
// agencies (see ListingAgencyRules); omitted means a private listing.
//
// Id is client-supplied and optional: the form generates one up front so it can attach uploaded
// photos (ConfirmMediaUpload) to that listing id before the listing row exists.
public record CreateListingCommand(
    Guid? Id,
    Guid RequestingUserId,
    Guid? AgencyId,
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
    ListingContact? Contact) : IRequest<ListingDto>, IListingWriteCommand;
