namespace Imova.Domain.Properties.Attributes;

public enum HouseType
{
    Individual = 1,
    Duplex = 2,
    Triplex = 3,
    Townhouse = 4,
    Villa = 5,
    Other = 6,
}

public enum BuildingMaterial
{
    Brick = 1,
    Panel = 2,
    ConcreteBlock = 3,
    // "Cotileț": sawn shell-limestone blocks, the traditional Moldovan wall material.
    LimestoneBlock = 4,
    Monolith = 5,
    Concrete = 6,
    Combined = 7,
    AeratedConcrete = 8,
    Wood = 9,
    // "Chirpici": clay-and-straw adobe.
    Adobe = 10,
    Other = 11,
}

// The state of a building's finish — shared by House, Apartment and Commercial, which use it
// instead of the coarser general PropertyCondition (kept for Garage and Room).
public enum FinishCondition
{
    ToBeDemolished = 1,
    IndividualDesign = 2,
    GrayStructure = 3,
    EuroRenovated = 4,
    WhiteStructure = 5,
    NeedsRepair = 6,
    Unfinished = 7,
    CosmeticRepair = 8,
    NoRepair = 9,
}

public enum HeatingSystem
{
    OwnBoiler = 1,
    Convector = 2,
    InfraredPanels = 3,
    DistrictHeating = 4,
    HeatPump = 5,
    SolarPanels = 6,
    Stove = 7,
    None = 8,
}

public enum HeatingEnergySource
{
    Gas = 1,
    Electricity = 2,
    Wood = 3,
    Combined = 4,
}

public enum HeatingDistribution
{
    Radiators = 1,
    UnderfloorHeating = 2,
    Air = 3,
}

public enum WaterSupply
{
    CentralNetwork = 1,
    Well = 2,
    DrilledWell = 3,
    Cistern = 4,
    None = 5,
}

public enum Sewerage
{
    Central = 1,
    SepticTank = 2,
    None = 3,
}

public enum FloorMaterial
{
    Parquet = 1,
    Laminate = 2,
    Tile = 3,
    Other = 4,
}

// What the attic (mansardă) is built/finished with.
public enum AtticMaterial
{
    Wood = 1,
    Drywall = 2,
    Osb = 3,
    Brick = 4,
    AeratedConcrete = 5,
    Other = 6,
}

public enum RoofMaterial
{
    Tile = 1,
    Metal = 2,
    Other = 3,
}

public enum WindowType
{
    Thermopane = 1,
    Wood = 2,
    Other = 3,
}

public enum RoadAccess
{
    Paved = 1,
    Gravel = 2,
    None = 3,
}

public enum CommercialSpaceType
{
    SportsSpace = 1,
    DentalSpace = 2,
    UniversalSpace = 3,
    ResortOrHotel = 4,
    AutoService = 5,
    BeautySalon = 6,
    ConferenceRoom = 7,
    RetailSpace = 8,
    IndustrialSpace = 9,
    Warehouse = 10,
    OfficeSpace = 11,
    FoodServiceSpace = 12,
}

public enum BathroomType
{
    Private = 1,
    Shared = 2,
}

// A deliberately simplified take on Moldovan building-series jargon ("seria 143", "135", ...),
// which is too niche for a listing form.
public enum ApartmentLayout
{
    Studio = 1,
    IndividualLayout = 2,
    // Soviet-era series such as the "hrușciovka".
    SovietEra = 3,
    // "Cămin" — former dormitory buildings.
    Dormitory = 4,
    Other = 5,
}

public enum HousingStockType
{
    Existing = 1,
    NewConstruction = 2,
}

public enum PlotType
{
    Agricultural = 1,
    WithPlantations = 2,
    Forest = 3,
    Garden = 4,
    Industrial = 5,
    NearLake = 6,
    ForConstruction = 7,
}

// Intravilan / extravilan: inside or outside a locality's built-up perimeter.
public enum LocationContext
{
    WithinTownLimits = 1,
    OutsideTownLimits = 2,
}

public enum ParkingType
{
    Garage = 1,
    ParkingSpot = 2,
    UndergroundParking = 3,
}
