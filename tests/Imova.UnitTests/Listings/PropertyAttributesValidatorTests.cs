using Imova.Application.Features.Listings.Attributes;
using Imova.Domain.Properties.Attributes;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class PropertyAttributesValidatorTests
{
    private static IEnumerable<string> ErrorsFor(PropertyAttributes attributes) =>
        PropertyAttributesValidator.Validate(attributes).Errors.Select(e => e.PropertyName);

    public static TheoryData<PropertyAttributes> CompleteOfEachType() =>
    [
        TestAttributes.CompleteApartment,
        TestAttributes.CompleteHouse,
        TestAttributes.CompleteLand,
        TestAttributes.CompleteCommercial,
        TestAttributes.CompleteGarage,
        TestAttributes.CompleteRoom,
    ];

    [Theory]
    [MemberData(nameof(CompleteOfEachType))]
    public void EveryType_WithEveryFieldFilledIn_IsValid(PropertyAttributes attributes)
    {
        Assert.Empty(ErrorsFor(attributes));
    }

    // --- Required fields per type ---

    [Fact]
    public void Apartment_WithNothingFilledIn_RequiresEveryNonOptionalField()
    {
        Assert.Equal(
            new[] { "Floor", "Rooms", "TotalFloors" },
            ErrorsFor(new ApartmentAttributes()).Distinct().Order());
    }

    [Fact]
    public void House_WithNothingFilledIn_RequiresEveryNonOptionalField()
    {
        Assert.Equal(
            new[] { "HouseFloors", "HouseType", "LandAreaM2", "Rooms" },
            ErrorsFor(new HouseAttributes()).Distinct().Order());
    }

    [Fact]
    public void Land_WithNothingFilledIn_RequiresOnlyPlotTypeAndLocationContext()
    {
        Assert.Equal(
            new[] { "LocationContext", "PlotType" },
            ErrorsFor(new LandAttributes()).Distinct().Order());
    }

    [Fact]
    public void Commercial_WithNothingFilledIn_RequiresEveryNonOptionalField()
    {
        Assert.Equal(
            new[] { "Bathrooms", "Floor", "SpaceType" },
            ErrorsFor(new CommercialAttributes()).Distinct().Order());
    }

    [Fact]
    public void GarageAndRoom_RequireOnlyTheirOneEnum()
    {
        Assert.Equal(["ParkingType"], ErrorsFor(new GarageAttributes()));
        Assert.Equal(["BathroomType"], ErrorsFor(new RoomAttributes()));
    }

    [Fact]
    public void OptionalFields_CanBeLeftEmpty()
    {
        Assert.Empty(ErrorsFor(TestAttributes.CompleteApartment with
        {
            Bathrooms = null, LivingAreaM2 = null, KitchenAreaM2 = null, HousingStockType = null, BuildingMaterial = null,
            FinishCondition = null, Layout = null, HeatingSystem = null, HeatingEnergySource = null,
            HeatingDistribution = null, GasSupply = null, FloorMaterial = null,
        }));
        Assert.Empty(ErrorsFor(TestAttributes.CompleteHouse with
        {
            KitchenAreaM2 = null, AtticAreaM2 = null, BasementAreaM2 = null, CeilingHeightM = null, AtticMaterial = null,
            BuildingMaterial = null, FinishCondition = null, LivingAreaM2 = null, HeatingSystem = null,
            HeatingEnergySource = null, HeatingDistribution = null, WaterSupply = null, Sewerage = null, GasSupply = null,
            FloorMaterial = null, RoofMaterial = null, WindowType = null,
        }));
        Assert.Empty(ErrorsFor(TestAttributes.CompleteLand with
        {
            SoilQualityScore = null, RoadAccess = null, GasPipelineAtBoundary = null, ElectricitySupplyAtBoundary = null,
            SewerageAtBoundary = null, IrrigationSystem = null, PhoneLineAvailable = null,
        }));
        Assert.Empty(ErrorsFor(TestAttributes.CompleteCommercial with
        {
            TotalFloorsInBuilding = null, WorkingAreaM2 = null, NumberOfOffices = null, PhoneLinesCount = null, ElectricalPower = null,
            FinishCondition = null, MainStreetAccess = null, GasSupply = null,
        }));
        Assert.Empty(ErrorsFor(TestAttributes.CompleteRoom with { RoommateCount = null }));
    }

    // --- Conditional heating details (Apartment and House share the rule) ---

    public static TheoryData<PropertyAttributes, string> HeatingCases()
    {
        var data = new TheoryData<PropertyAttributes, string>();
        foreach (var system in Enum.GetValues<HeatingSystem>())
        {
            data.Add(TestAttributes.CompleteApartment with { HeatingSystem = system, HeatingEnergySource = null, HeatingDistribution = null }, system.ToString());
            data.Add(TestAttributes.CompleteHouse with { HeatingSystem = system, HeatingEnergySource = null, HeatingDistribution = null }, system.ToString());
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(HeatingCases))]
    public void HeatingDetails_AreRequiredExactlyWhenTheHeatingSystemHasThem(PropertyAttributes withoutDetails, string system)
    {
        // Solar panels are their own energy source, so they only take a distribution.
        var requiresEnergySource = system is "OwnBoiler" or "HeatPump";
        var requiresDistribution = system is "OwnBoiler" or "HeatPump" or "SolarPanels";
        var withDetails = withoutDetails switch
        {
            ApartmentAttributes a => (PropertyAttributes)(a with { HeatingEnergySource = HeatingEnergySource.Electricity, HeatingDistribution = HeatingDistribution.Air }),
            HouseAttributes h => h with { HeatingEnergySource = HeatingEnergySource.Electricity, HeatingDistribution = HeatingDistribution.Air },
            _ => throw new InvalidOperationException(),
        };

        // Missing a required detail and supplying one that doesn't apply both fail on that field.
        var missing = new List<string>();
        var superfluous = new List<string>();
        (requiresDistribution ? missing : superfluous).Add("HeatingDistribution");
        (requiresEnergySource ? missing : superfluous).Add("HeatingEnergySource");

        Assert.Equal(missing, ErrorsFor(withoutDetails).Order());
        Assert.Equal(superfluous, ErrorsFor(withDetails).Order());
    }

    [Theory]
    [MemberData(nameof(ApartmentAndHouse))]
    public void SolarPanels_TakeADistributionButNoEnergySource(PropertyAttributes complete)
    {
        var solar = complete switch
        {
            ApartmentAttributes a => (PropertyAttributes)(a with { HeatingSystem = HeatingSystem.SolarPanels, HeatingEnergySource = null, HeatingDistribution = HeatingDistribution.UnderfloorHeating }),
            HouseAttributes h => h with { HeatingSystem = HeatingSystem.SolarPanels, HeatingEnergySource = null, HeatingDistribution = HeatingDistribution.UnderfloorHeating },
            _ => throw new InvalidOperationException(),
        };
        var solarWithSource = solar switch
        {
            ApartmentAttributes a => (PropertyAttributes)(a with { HeatingEnergySource = HeatingEnergySource.Electricity }),
            HouseAttributes h => h with { HeatingEnergySource = HeatingEnergySource.Electricity },
            _ => throw new InvalidOperationException(),
        };

        Assert.Empty(ErrorsFor(solar));
        Assert.Equal(["HeatingEnergySource"], ErrorsFor(solarWithSource));
    }

    public static TheoryData<PropertyAttributes> ApartmentAndHouse() =>
        new() { TestAttributes.CompleteApartment, TestAttributes.CompleteHouse };

    [Fact]
    public void HeatingDetails_WithNoHeatingSystem_AreRejected()
    {
        Assert.Equal(
            new[] { "HeatingDistribution", "HeatingEnergySource" },
            ErrorsFor(TestAttributes.CompleteApartment with { HeatingSystem = null }).Order());
        Assert.Equal(
            new[] { "HeatingDistribution", "HeatingEnergySource" },
            ErrorsFor(TestAttributes.CompleteHouse with { HeatingSystem = null }).Order());
    }

    [Fact]
    public void OptionalEnums_StillRejectUnknownValues()
    {
        Assert.Equal(
            new[] { "BuildingMaterial", "FloorMaterial", "RoofMaterial", "Sewerage", "WaterSupply", "WindowType" },
            ErrorsFor(TestAttributes.CompleteHouse with
            {
                BuildingMaterial = (BuildingMaterial)42, FloorMaterial = (FloorMaterial)42, RoofMaterial = (RoofMaterial)42,
                Sewerage = (Sewerage)42, WaterSupply = (WaterSupply)42, WindowType = (WindowType)42,
            }).Order());
        Assert.Equal(
            new[] { "HousingStockType", "Layout" },
            ErrorsFor(TestAttributes.CompleteApartment with { HousingStockType = (HousingStockType)42, Layout = (ApartmentLayout)42 }).Order());
        Assert.Equal(["RoadAccess"], ErrorsFor(TestAttributes.CompleteLand with { RoadAccess = (RoadAccess)42 }));
        Assert.Equal(["FinishCondition"], ErrorsFor(TestAttributes.CompleteCommercial with { FinishCondition = (FinishCondition)42 }));
    }

    [Fact]
    public void Heating_RequiresEnergySource_IsTrueOnlyForBoilerAndHeatPump()
    {
        Assert.Equal(
            [HeatingSystem.OwnBoiler, HeatingSystem.HeatPump],
            Enum.GetValues<HeatingSystem>().Where(s => Heating.RequiresEnergySource(s)));
        Assert.False(Heating.RequiresEnergySource(null));
    }

    [Fact]
    public void Heating_RequiresDistribution_IsTrueOnlyForBoilerHeatPumpAndSolar()
    {
        Assert.Equal(
            [HeatingSystem.OwnBoiler, HeatingSystem.HeatPump, HeatingSystem.SolarPanels],
            Enum.GetValues<HeatingSystem>().Where(s => Heating.RequiresDistribution(s)));
        Assert.False(Heating.RequiresDistribution(null));
    }

    // --- Other per-type conditional fields ---

    [Theory]
    [InlineData(PlotType.WithPlantations)]
    [InlineData(PlotType.ForConstruction)]
    [InlineData(PlotType.Forest)]
    public void Land_SoilQualityScore_OnlyAppliesToAgriculturalPlots(PlotType plotType)
    {
        Assert.Equal(["SoilQualityScore"], ErrorsFor(TestAttributes.CompleteLand with { PlotType = plotType }));
        Assert.Empty(ErrorsFor(TestAttributes.CompleteLand with { PlotType = plotType, SoilQualityScore = null }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Land_SoilQualityScore_IsAOneToHundredIndex(int score)
    {
        Assert.Equal(["SoilQualityScore"], ErrorsFor(TestAttributes.CompleteLand with { SoilQualityScore = score }));
    }

    [Theory]
    [InlineData(CommercialSpaceType.RetailSpace)]
    [InlineData(CommercialSpaceType.Warehouse)]
    public void Commercial_NumberOfOffices_OnlyAppliesToOfficeSpace(CommercialSpaceType spaceType)
    {
        Assert.Equal(["NumberOfOffices"], ErrorsFor(TestAttributes.CompleteCommercial with { SpaceType = spaceType }));
        Assert.Empty(ErrorsFor(TestAttributes.CompleteCommercial with { SpaceType = spaceType, NumberOfOffices = null }));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(4)]
    public void Commercial_Floor_AllowsBasementLevels(int floor)
    {
        Assert.Empty(ErrorsFor(TestAttributes.CompleteCommercial with { Floor = floor }));
    }

    [Fact]
    public void FloorAboveTheBuildingsFloorCount_IsInvalid()
    {
        Assert.Contains(
            PropertyAttributesValidator.Validate(TestAttributes.CompleteApartment with { Floor = 10, TotalFloors = 9 }).Errors,
            e => e.ErrorMessage == "Floor cannot be greater than TotalFloors.");
        Assert.Contains(
            PropertyAttributesValidator.Validate(TestAttributes.CompleteCommercial with { Floor = 6, TotalFloorsInBuilding = 5 }).Errors,
            e => e.ErrorMessage == "Floor cannot be greater than TotalFloorsInBuilding.");
    }

    [Fact]
    public void YesNoAnswers_AcceptFalse()
    {
        Assert.Empty(ErrorsFor(TestAttributes.CompleteHouse with { GasSupply = false }));
        Assert.Empty(ErrorsFor(TestAttributes.CompleteCommercial with { MainStreetAccess = false }));
        Assert.Empty(ErrorsFor(TestAttributes.CompleteLand with { PhoneLineAvailable = false, IrrigationSystem = false }));
    }

    [Fact]
    public void House_RejectsOutOfRangeNumbersAndUnknownEnums()
    {
        var house = TestAttributes.CompleteHouse with
        {
            LivingAreaM2 = 0, KitchenAreaM2 = -1, CeilingHeightM = 12m, HouseFloors = 11, AtticMaterial = (AtticMaterial)42,
        };

        Assert.Equal(
            new[] { "AtticMaterial", "CeilingHeightM", "HouseFloors", "KitchenAreaM2", "LivingAreaM2" },
            ErrorsFor(house).Order());
    }

    [Fact]
    public void Commercial_RejectsOverlongElectricalPower()
    {
        Assert.Equal(["ElectricalPower"], ErrorsFor(TestAttributes.CompleteCommercial with { ElectricalPower = new string('x', 51) }));
    }
}
