using Imova.Domain.Common;
using Imova.Domain.Listings;

namespace Imova.Domain.Amenities;

// Seeded reference data (see AmenityConfiguration's HasData), not a hardcoded enum, so the list
// can grow with a migration instead of a code change to every switch over it. Key is the stable
// identifier the frontend translates; LabelRo is the canonical display label.
public sealed class Amenity : Entity
{
    public const string FurnishedKey = "furnished";

    public Amenity(Guid id, string key, string labelRo, AmenityCategory category = AmenityCategory.General)
        : base(id)
    {
        Key = key;
        LabelRo = labelRo;
        Category = category;
    }

    public string Key { get; private set; }

    public string LabelRo { get; private set; }

    public AmenityCategory Category { get; private set; }

    // A rental states furnishing through RentalDetails.FurnishedStatus (unfurnished / partially /
    // fully), so the plain "furnished" amenity isn't offered for rentals — two answers to the same
    // question could disagree. Everything else is selectable for any transaction type.
    public static bool IsSelectableFor(string key, TransactionType transactionType) =>
        !(transactionType == TransactionType.Rent && key == FurnishedKey);
}
