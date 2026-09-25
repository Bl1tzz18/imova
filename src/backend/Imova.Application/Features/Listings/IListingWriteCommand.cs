using System.Text.Json;
using Imova.Domain.Listings;
using Imova.Domain.Properties;

namespace Imova.Application.Features.Listings;

// The full Property + Listing payload the listing form submits — shared by CreateListingCommand
// and UpdateListingCommand (the edit page reuses the exact same multi-step form, so both carry the
// same field set) so ListingWriteValidator/ListingWriteSupport can serve both.
public interface IListingWriteCommand
{
    // --- Property (the physical asset) ---
    PropertyType PropertyType { get; }

    decimal TotalAreaM2 { get; }

    int? YearBuilt { get; }

    PropertyCondition? Condition { get; }

    // Raw JSON on purpose: which schema applies depends on PropertyType, so it can only be parsed
    // once PropertyType is known — see PropertyAttributesJson.TryParse.
    JsonElement? TypeSpecificAttributes { get; }

    IReadOnlyList<Guid>? AmenityIds { get; }

    // What the property is close to (school, park, ...) — optional for every type.
    IReadOnlyList<Guid>? ProximityIds { get; }

    // No Latitude/Longitude on purpose — coordinates are derived server-side via
    // IGeocodingService rather than trusting client-supplied ones for a listing's real location.
    string Country { get; }

    Guid RaionId { get; }

    Guid? LocalitateId { get; }

    Guid? ChisinauSectorId { get; }

    // Required (see ListingWriteValidator) — still nullable at the type level since a malformed
    // request could send null; validation, not the C# type, is what enforces it.
    string? StreetAddress { get; }

    string? BuildingNumber { get; }

    // --- Listing (the offer) ---
    TransactionType TransactionType { get; }

    string Title { get; }

    string Description { get; }

    decimal Price { get; }

    Currency Currency { get; }

    bool IsNegotiable { get; }

    // Only for TransactionType.Rent (defaults to an empty RentalDetails when omitted); must be
    // null for a sale.
    RentalDetails? RentalDetails { get; }

    // --- Contact (the form's last step) --- required on every create/edit.
    ListingContact? Contact { get; }
}
