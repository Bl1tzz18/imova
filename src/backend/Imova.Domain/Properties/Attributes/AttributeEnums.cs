namespace Imova.Domain.Properties.Attributes;

public enum HeatingType
{
    Centralized = 1,
    Autonomous = 2,
    Other = 3,
}

public enum ConstructionType
{
    Brick = 1,
    Stone = 2,
    Wood = 3,
    Other = 4,
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
