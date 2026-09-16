namespace Imova.Domain.Properties;

// Which of the optional detail fields apply to a given PropertyType (and, for PetsAllowed,
// ListingType), and whether they're required, optional, or not applicable at all — e.g. an
// apartment needs its own Floor plus the building's TotalFloors, a house only needs
// TotalFloors (a house isn't "on" a floor of something bigger), and land needs neither.
//
// CreatePropertyValidator enforces this server-side. The frontend mirrors this table in
// src/lib/property/fieldRules.ts to drive the add-listing form — keep the two in sync.
public static class PropertyFieldRules
{
    public static FieldRequirement Area(PropertyType type) =>
        type == PropertyType.Room ? FieldRequirement.Optional : FieldRequirement.Required;

    public static FieldRequirement Rooms(PropertyType type) => type switch
    {
        PropertyType.Apartment or PropertyType.House => FieldRequirement.Required,
        PropertyType.Commercial => FieldRequirement.Optional,
        _ => FieldRequirement.Hidden,
    };

    public static FieldRequirement Bathrooms(PropertyType type) => type switch
    {
        PropertyType.Apartment or PropertyType.House or PropertyType.Commercial => FieldRequirement.Optional,
        _ => FieldRequirement.Hidden,
    };

    public static FieldRequirement Floor(PropertyType type) => type switch
    {
        PropertyType.Apartment => FieldRequirement.Required,
        PropertyType.Commercial or PropertyType.Garage or PropertyType.Room => FieldRequirement.Optional,
        _ => FieldRequirement.Hidden,
    };

    public static FieldRequirement TotalFloors(PropertyType type) => type switch
    {
        PropertyType.Apartment or PropertyType.House => FieldRequirement.Required,
        PropertyType.Commercial or PropertyType.Room => FieldRequirement.Optional,
        _ => FieldRequirement.Hidden,
    };

    public static FieldRequirement YearBuilt(PropertyType type) => type switch
    {
        PropertyType.Land or PropertyType.Room => FieldRequirement.Hidden,
        _ => FieldRequirement.Optional,
    };

    public static FieldRequirement Furnished(PropertyType type) => type switch
    {
        PropertyType.Apartment or PropertyType.House or PropertyType.Room => FieldRequirement.Optional,
        _ => FieldRequirement.Hidden,
    };

    public static FieldRequirement ParkingAvailable(PropertyType type) => type switch
    {
        PropertyType.Apartment or PropertyType.House or PropertyType.Commercial => FieldRequirement.Optional,
        _ => FieldRequirement.Hidden,
    };

    // Only meaningful for a rental — hidden entirely for a sale listing.
    public static FieldRequirement PetsAllowed(PropertyType type, ListingType listingType)
    {
        if (listingType != ListingType.Rent)
        {
            return FieldRequirement.Hidden;
        }

        return type switch
        {
            PropertyType.Apartment or PropertyType.House or PropertyType.Room => FieldRequirement.Optional,
            _ => FieldRequirement.Hidden,
        };
    }
}
