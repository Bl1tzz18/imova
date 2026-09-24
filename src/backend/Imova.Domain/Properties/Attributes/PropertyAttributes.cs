namespace Imova.Domain.Properties.Attributes;

// The PropertyType-specific part of a Property, stored as one JSONB column rather than a column
// per field. Each PropertyType has exactly one concrete record below, so (de)serialization and
// validation always know exactly which fields to expect. Fields are nullable where a value can
// legitimately be missing — either optional by design, or absent on listings created before this
// schema existed; which ones are *required* for a new write is the Application layer's
// validators' call (see PropertyAttributesValidator), not the type system's.
public abstract record PropertyAttributes
{
    // A method rather than a property on purpose: it must not show up as a serialized JSON field.
    public abstract PropertyType GetPropertyType();

    // An attributes object with nothing filled in, for the given type.
    public static PropertyAttributes EmptyFor(PropertyType propertyType) => propertyType switch
    {
        PropertyType.Apartment => new ApartmentAttributes(),
        PropertyType.House => new HouseAttributes(),
        PropertyType.Land => new LandAttributes(),
        PropertyType.Commercial => new CommercialAttributes(),
        PropertyType.Garage => new GarageAttributes(),
        PropertyType.Room => new RoomAttributes(),
        _ => throw new ArgumentOutOfRangeException(nameof(propertyType), propertyType, null),
    };
}

public sealed record ApartmentAttributes(
    int? Rooms = null,
    int? Floor = null,
    int? TotalFloors = null,
    int? Bathrooms = null,
    HeatingType? HeatingType = null) : PropertyAttributes
{
    public override PropertyType GetPropertyType() => PropertyType.Apartment;
}

// Everything a house listing describes about the building itself — identical for sale and rent.
// Replaces the earlier ConstructionType/Utilities fields (see the AddHouseDetailsAndAmenity
// Categories migration). Its HouseCondition stands in for Property.Condition, which stays null
// for a House.
public sealed record HouseAttributes(
    // Type & structure
    int? Rooms = null,
    HouseType? HouseType = null,
    BuildingMaterial? BuildingMaterial = null,
    HouseCondition? HouseCondition = null,
    int? HouseFloors = null,
    decimal? CeilingHeightM = null,
    // Areas (m²)
    decimal? LivingAreaM2 = null,
    decimal? LandAreaM2 = null,
    decimal? KitchenAreaM2 = null,
    decimal? AtticAreaM2 = null,
    decimal? BasementAreaM2 = null,
    // Systems & utilities
    HeatingSystem? HeatingSystem = null,
    // Only meaningful (and then required) for heating that has its own energy source and
    // distribution — see RequiresHeatingDetails.
    HeatingEnergySource? HeatingEnergySource = null,
    HeatingDistribution? HeatingDistribution = null,
    WaterSupply? WaterSupply = null,
    Sewerage? Sewerage = null,
    bool GasSupply = false,
    // Finishing materials
    FloorMaterial? FloorMaterial = null,
    // Free text: attic finishes vary too much to enumerate usefully.
    string? AtticMaterial = null,
    RoofMaterial? RoofMaterial = null,
    WindowType? WindowType = null) : PropertyAttributes
{
    public override PropertyType GetPropertyType() => PropertyType.House;

    // A boiler, heat pump or solar setup has a fuel/energy source and a way heat is distributed
    // around the house; district heating, convectors, IR panels, a stove, or no heating don't.
    public static bool RequiresHeatingDetails(HeatingSystem? heatingSystem) =>
        heatingSystem is Attributes.HeatingSystem.OwnBoiler
            or Attributes.HeatingSystem.HeatPump
            or Attributes.HeatingSystem.SolarPanels;
}

public sealed record LandAttributes(
    LandDesignation? LandDesignation = null,
    RoadAccess? RoadAccess = null,
    BoundaryUtilities? UtilitiesAtBoundary = null) : PropertyAttributes
{
    public override PropertyType GetPropertyType() => PropertyType.Land;
}

public sealed record BoundaryUtilities(bool Water, bool Electricity, bool Gas);

public sealed record CommercialAttributes(
    CommercialSpaceType? SpaceType = null,
    int? Floor = null,
    bool MainStreetAccess = false,
    // Free text on purpose (e.g. "three-phase 380V", "15 kW") — too many real-world formats to
    // enumerate usefully.
    string? ElectricalPower = null) : PropertyAttributes
{
    public override PropertyType GetPropertyType() => PropertyType.Commercial;
}

public sealed record GarageAttributes(GarageType? GarageType = null) : PropertyAttributes
{
    public override PropertyType GetPropertyType() => PropertyType.Garage;
}

public sealed record RoomAttributes(
    BathroomType? PrivateOrSharedBathroom = null,
    int? RoommateCount = null) : PropertyAttributes
{
    public override PropertyType GetPropertyType() => PropertyType.Room;
}
