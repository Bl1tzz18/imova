namespace Imova.Domain.Properties.Attributes;

public static class Heating
{
    // A boiler or heat pump runs on a fuel/energy source; solar panels are their own source, so
    // asking for one would be redundant. District heating, convectors, IR panels, a stove, or no
    // heating have neither detail. Applies to every type with a HeatingSystem (House, Apartment).
    public static bool RequiresEnergySource(HeatingSystem? heatingSystem) =>
        heatingSystem is HeatingSystem.OwnBoiler or HeatingSystem.HeatPump;

    // How heat is distributed around the home (radiators, underfloor, air) — meaningful for a
    // boiler, a heat pump and solar heating alike.
    public static bool RequiresDistribution(HeatingSystem? heatingSystem) =>
        heatingSystem is HeatingSystem.OwnBoiler or HeatingSystem.HeatPump or HeatingSystem.SolarPanels;
}
