namespace Imova.Domain.Listings;

// Placeholder for sale-only offer terms — only present when TransactionType is Sale. Kept
// deliberately minimal until there's a real requirement to extend it.
public sealed record SaleDetails(string? OwnershipDocumentType = null);
