namespace Imova.Contracts.Listings;

// A listing's contact phone number, as entered — given out one request at a time (rate-limited),
// never in the listing itself. See POST /api/v1/listings/{id}/contact/phone.
public record ListingPhoneDto(string Phone);
