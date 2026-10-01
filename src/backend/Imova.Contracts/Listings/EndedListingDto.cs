namespace Imova.Contracts.Listings;

// What anyone may still see of a listing that is no longer up (sold, rented, expired, or taken down
// by its owner) — a link to it then says so instead of a 404, and offers similar listings. Only what
// identifies the offer: no photos, description, street address or contact. Status: Sold | Rented |
// Expired | Archived. Location: raion, then locality or Chișinău neighborhood, when set.
public record EndedListingDto(
    Guid Id,
    string Status,
    string TransactionType,
    string PropertyType,
    string Title,
    PriceDto Price,
    decimal TotalAreaM2,
    Guid RaionId,
    string RaionName,
    string? LocalitateName,
    string? ChisinauSectorName,
    DateTimeOffset EndedAt);
