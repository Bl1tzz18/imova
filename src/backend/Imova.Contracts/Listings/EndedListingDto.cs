namespace Imova.Contracts.Listings;

// What anyone may still see of a listing that is no longer up (sold, rented, expired, or taken down
// by its owner) — a link to it then says so instead of a 404, and offers similar listings. Only what
// identifies the offer: no photos, description, street address or contact. Status: Sold | Rented |
// Expired | Archived. Location: raion, then locality or Chișinău neighborhood, when set. Number is
// the short public listing number — with the title, the listing's web address.
public record EndedListingDto(
    Guid Id,
    long Number,
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
