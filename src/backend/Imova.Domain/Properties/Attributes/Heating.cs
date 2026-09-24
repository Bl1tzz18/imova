namespace Imova.Domain.Properties.Attributes;

public static class Heating
{
    // A boiler, heat pump or solar setup has a fuel/energy source and a way heat is distributed
    // around the home; district heating, convectors, IR panels, a stove, or no heating don't.
    // Applies to every type with a HeatingSystem (House, Apartment).
    public static bool RequiresDetails(HeatingSystem? heatingSystem) =>
        heatingSystem is HeatingSystem.OwnBoiler or HeatingSystem.HeatPump or HeatingSystem.SolarPanels;
}
