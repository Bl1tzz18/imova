namespace Imova.Domain.Listings;

// Same integer values as the old ListingType enum it replaces, so migrated rows map 1:1.
public enum TransactionType
{
    Rent = 1,
    Sale = 2,
}
