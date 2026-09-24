using Imova.Domain.Properties.Attributes;

namespace Imova.UnitTests.TestSupport;

// One attributes object per PropertyType with every section of the details form filled in —
// including the conditional fields their controlling values require (boiler heating details,
// the agricultural soil score, the office count).
internal static class TestAttributes
{
    public static readonly ApartmentAttributes CompleteApartment = new(
        HousingStockType: HousingStockType.NewConstruction,
        BuildingMaterial: BuildingMaterial.Monolith,
        FinishCondition: FinishCondition.EuroRenovated,
        Layout: ApartmentLayout.IndividualLayout,
        Rooms: 2,
        Floor: 3,
        TotalFloors: 9,
        Bathrooms: 1,
        LivingAreaM2: 38m,
        KitchenAreaM2: 12m,
        HeatingSystem: HeatingSystem.OwnBoiler,
        HeatingEnergySource: HeatingEnergySource.Gas,
        HeatingDistribution: HeatingDistribution.Radiators,
        GasSupply: true,
        FloorMaterial: FloorMaterial.Laminate);

    public static readonly HouseAttributes CompleteHouse = new(
        Rooms: 5,
        HouseType: HouseType.Individual,
        BuildingMaterial: BuildingMaterial.Brick,
        FinishCondition: FinishCondition.EuroRenovated,
        HouseFloors: 2,
        CeilingHeightM: 2.8m,
        LivingAreaM2: 160m,
        LandAreaM2: 600m,
        KitchenAreaM2: 18m,
        AtticAreaM2: 40m,
        BasementAreaM2: 30m,
        HeatingSystem: HeatingSystem.OwnBoiler,
        HeatingEnergySource: HeatingEnergySource.Gas,
        HeatingDistribution: HeatingDistribution.UnderfloorHeating,
        WaterSupply: WaterSupply.CentralNetwork,
        Sewerage: Sewerage.Central,
        GasSupply: true,
        FloorMaterial: FloorMaterial.Parquet,
        AtticMaterial: AtticMaterial.Wood,
        RoofMaterial: RoofMaterial.Tile,
        WindowType: WindowType.Thermopane);

    public static readonly LandAttributes CompleteLand = new(
        PlotType: PlotType.Agricultural,
        LocationContext: LocationContext.OutsideTownLimits,
        SoilQualityScore: 64,
        RoadAccess: RoadAccess.Gravel,
        GasPipelineAtBoundary: false,
        ElectricitySupplyAtBoundary: true,
        SewerageAtBoundary: false,
        IrrigationSystem: true,
        PhoneLineAvailable: false);

    public static readonly CommercialAttributes CompleteCommercial = new(
        SpaceType: CommercialSpaceType.OfficeSpace,
        FinishCondition: FinishCondition.CosmeticRepair,
        Floor: -1,
        TotalFloorsInBuilding: 5,
        WorkingAreaM2: 110m,
        NumberOfOffices: 6,
        Bathrooms: 2,
        PhoneLinesCount: 4,
        MainStreetAccess: true,
        ElectricalPower: "three-phase 380V",
        GasSupply: false);

    public static readonly GarageAttributes CompleteGarage = new(ParkingType.UndergroundParking);

    public static readonly RoomAttributes CompleteRoom = new(BathroomType.Shared, RoommateCount: 2);
}
