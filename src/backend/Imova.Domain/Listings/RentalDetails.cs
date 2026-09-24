using Imova.Domain.Properties;

namespace Imova.Domain.Listings;

// Rental-only offer terms — only present when TransactionType is Rent. Lives on Listing, not
// Property: deposit, lease length and pet policy are terms of this particular offer. (Whether the
// home is furnished is the property's "furnished" amenity, for sale and rent alike.)
public sealed record RentalDetails(
    int? MinLeasePeriodMonths = null,
    decimal? SecurityDepositAmount = null,
    bool UtilitiesIncluded = false,
    DateTime? AvailableFrom = null,
    // A required Yes/No for homes (see PetsApplyTo); null — not asked — for everything else.
    bool? PetsAllowed = null)
{
    // Pets are only a question for somewhere people live, not a rented garage, plot or office.
    public static bool PetsApplyTo(PropertyType propertyType) =>
        propertyType is PropertyType.Apartment or PropertyType.House or PropertyType.Room;
}
