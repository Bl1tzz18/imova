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

// Apartment, House and Commercial share the finish-condition, building-material, heating and
// floor-material vocabularies (see AttributeEnums); a House and an Apartment also share the
// conditional heating-details rule in Heating.
public sealed record ApartmentAttributes(
    // Type & structure
    HousingStockType? HousingStockType = null,
    BuildingMaterial? BuildingMaterial = null,
    FinishCondition? FinishCondition = null,
    ApartmentLayout? Layout = null,
    int? Rooms = null,
    int? Floor = null,
    int? TotalFloors = null,
    int? Bathrooms = null,
    // Areas (m²) — the total is Property.TotalAreaM2
    decimal? LivingAreaM2 = null,
    decimal? KitchenAreaM2 = null,
    // Systems & utilities
    HeatingSystem? HeatingSystem = null,
    HeatingEnergySource? HeatingEnergySource = null,
    HeatingDistribution? HeatingDistribution = null,
    bool? GasSupply = null,
    // Finishing materials
    FloorMaterial? FloorMaterial = null) : PropertyAttributes
{
    public override PropertyType GetPropertyType() => PropertyType.Apartment;
}

// Everything a house listing describes about the building itself — identical for sale and rent.
// Its FinishCondition stands in for Property.Condition, which stays null for a House.
public sealed record HouseAttributes(
    // Type & structure
    int? Rooms = null,
    HouseType? HouseType = null,
    BuildingMaterial? BuildingMaterial = null,
    FinishCondition? FinishCondition = null,
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
    // distribution — see Heating.RequiresDetails.
    HeatingEnergySource? HeatingEnergySource = null,
    HeatingDistribution? HeatingDistribution = null,
    WaterSupply? WaterSupply = null,
    Sewerage? Sewerage = null,
    // Nullable so "not answered" is distinguishable from "no" — asked as an optional Yes/No.
    bool? GasSupply = null,
    // Finishing materials
    FloorMaterial? FloorMaterial = null,
    // Optional: not every house has an attic.
    AtticMaterial? AtticMaterial = null,
    RoofMaterial? RoofMaterial = null,
    WindowType? WindowType = null) : PropertyAttributes
{
    public override PropertyType GetPropertyType() => PropertyType.House;
}

// The plot's own area is Property.TotalAreaM2 — there is no separate land-area field here.
public sealed record LandAttributes(
    // Type & area
    PlotType? PlotType = null,
    LocationContext? LocationContext = null,
    // The "bonitate" soil-quality score — only for agricultural plots (see RequiresSoilQuality).
    int? SoilQualityScore = null,
    // Utilities & access (optional Yes/No answers)
    RoadAccess? RoadAccess = null,
    bool? GasPipelineAtBoundary = null,
    bool? ElectricitySupplyAtBoundary = null,
    bool? SewerageAtBoundary = null,
    bool? IrrigationSystem = null,
    bool? PhoneLineAvailable = null) : PropertyAttributes
{
    public override PropertyType GetPropertyType() => PropertyType.Land;

    public static bool AllowsSoilQuality(PlotType? plotType) => plotType == Attributes.PlotType.Agricultural;
}

public sealed record CommercialAttributes(
    // Type & structure
    CommercialSpaceType? SpaceType = null,
    FinishCondition? FinishCondition = null,
    // Negative for basement levels: -1 = basement (subsol), 0 = semi-basement (demisol).
    int? Floor = null,
    int? TotalFloorsInBuilding = null,
    // Areas (m²) — the total is Property.TotalAreaM2
    decimal? WorkingAreaM2 = null,
    // Only for office space (see AllowsNumberOfOffices).
    int? NumberOfOffices = null,
    // Systems & utilities
    int? Bathrooms = null,
    int? PhoneLinesCount = null,
    bool? MainStreetAccess = null,
    // Free text on purpose (e.g. "three-phase 380V", "15 kW") — too many real-world formats to
    // enumerate usefully.
    string? ElectricalPower = null,
    bool? GasSupply = null) : PropertyAttributes
{
    public override PropertyType GetPropertyType() => PropertyType.Commercial;

    public static bool AllowsNumberOfOffices(CommercialSpaceType? spaceType) =>
        spaceType == CommercialSpaceType.OfficeSpace;
}

public sealed record GarageAttributes(ParkingType? ParkingType = null) : PropertyAttributes
{
    public override PropertyType GetPropertyType() => PropertyType.Garage;
}

// The room's own area is Property.TotalAreaM2.
public sealed record RoomAttributes(
    BathroomType? BathroomType = null,
    int? RoommateCount = null) : PropertyAttributes
{
    public override PropertyType GetPropertyType() => PropertyType.Room;
}
