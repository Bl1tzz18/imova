namespace Imova.Domain.Properties.Attributes;

public enum HeatingType
{
    Centralized = 1,
    Autonomous = 2,
    Other = 3,
}

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

// More granular than the general PropertyCondition, which a House doesn't use.
public enum HouseCondition
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

// Intravilan/Extravilan: inside/outside a locality's built-up perimeter (Moldovan cadastral terms).
public enum LandDesignation
{
    Intravilan = 1,
    Extravilan = 2,
    Agricultural = 3,
    Construction = 4,
}

public enum RoadAccess
{
    Paved = 1,
    Gravel = 2,
    None = 3,
}

public enum CommercialSpaceType
{
    Office = 1,
    Retail = 2,
    Warehouse = 3,
    HoReCa = 4,
}

public enum GarageType
{
    Underground = 1,
    Box = 2,
    Individual = 3,
}

public enum BathroomType
{
    Private = 1,
    Shared = 2,
}
