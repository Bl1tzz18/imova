namespace Imova.Domain.Listings;

// Rental-only offer terms — only present when TransactionType is Rent. Lives on Listing, not
// Property: furnishing, deposit, and pet policy are terms of this particular offer.
public sealed record RentalDetails(
    int? MinLeasePeriodMonths = null,
    decimal? SecurityDepositAmount = null,
    bool UtilitiesIncluded = false,
    FurnishedStatus FurnishedStatus = FurnishedStatus.Unfurnished,
    DateTime? AvailableFrom = null,
    bool PetsAllowed = false);
