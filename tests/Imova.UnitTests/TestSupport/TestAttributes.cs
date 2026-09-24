using Imova.Domain.Properties.Attributes;

namespace Imova.UnitTests.TestSupport;

internal static class TestAttributes
{
    // A House with every section of the details form filled in, including the heating details its
    // OwnBoiler heating system requires.
    public static readonly HouseAttributes CompleteHouse = new(
        Rooms: 5,
        HouseType: HouseType.Individual,
        BuildingMaterial: BuildingMaterial.Brick,
        HouseCondition: HouseCondition.EuroRenovated,
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
}
