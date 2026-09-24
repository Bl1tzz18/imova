using Imova.Application.Features.Listings.Attributes;
using Imova.Domain.Properties.Attributes;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class PropertyAttributesValidatorTests
{
    private static IEnumerable<string> ErrorsFor(PropertyAttributes attributes) =>
        PropertyAttributesValidator.Validate(attributes).Errors.Select(e => e.PropertyName);

    [Fact]
    public void Apartment_WithRoomsFloorAndTotalFloors_IsValid()
    {
        Assert.Empty(ErrorsFor(new ApartmentAttributes(Rooms: 2, Floor: 3, TotalFloors: 9)));
    }

    [Fact]
    public void Apartment_WithNothingFilledIn_RequiresRoomsFloorAndTotalFloors()
    {
        Assert.Equal(
            new[] { "Floor", "Rooms", "TotalFloors" },
            ErrorsFor(new ApartmentAttributes()).Distinct().Order());
    }

    [Fact]
    public void Apartment_FloorAboveTotalFloors_IsInvalid()
    {
        var result = PropertyAttributesValidator.Validate(new ApartmentAttributes(Rooms: 2, Floor: 10, TotalFloors: 9));

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Floor cannot be greater than TotalFloors.");
    }

    [Fact]
    public void Apartment_WithUndefinedHeatingType_IsInvalid()
    {
        Assert.Contains("HeatingType", ErrorsFor(new ApartmentAttributes(2, 3, 9, HeatingType: (HeatingType)42)));
    }

    [Fact]
    public void House_WithEveryRequiredFieldFilledIn_IsValid()
    {
        Assert.Empty(ErrorsFor(TestAttributes.CompleteHouse));
    }

    [Fact]
    public void House_WithNothingFilledIn_RequiresEveryNonOptionalField()
    {
        Assert.Equal(
            new[]
            {
                "BuildingMaterial", "FloorMaterial", "GasSupply", "HeatingSystem", "HouseCondition", "HouseFloors", "HouseType",
                "LandAreaM2", "LivingAreaM2", "RoofMaterial", "Rooms", "Sewerage", "WaterSupply", "WindowType",
            },
            ErrorsFor(new HouseAttributes()).Distinct().Order());
    }

    [Fact]
    public void House_OptionalAreasCeilingAndAtticMaterial_CanBeLeftEmpty()
    {
        var house = TestAttributes.CompleteHouse with
        {
            KitchenAreaM2 = null, AtticAreaM2 = null, BasementAreaM2 = null, CeilingHeightM = null, AtticMaterial = null,
        };

        Assert.Empty(ErrorsFor(house));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void House_GasSupply_AcceptsAnExplicitYesOrNo(bool gasSupply)
    {
        Assert.Empty(ErrorsFor(TestAttributes.CompleteHouse with { GasSupply = gasSupply }));
    }

    [Fact]
    public void House_GasSupply_MustBeAnswered()
    {
        Assert.Equal(["GasSupply"], ErrorsFor(TestAttributes.CompleteHouse with { GasSupply = null }));
    }

    [Theory]
    [InlineData(HeatingSystem.OwnBoiler)]
    [InlineData(HeatingSystem.HeatPump)]
    [InlineData(HeatingSystem.SolarPanels)]
    public void House_HeatingWithItsOwnSource_RequiresEnergySourceAndDistribution(HeatingSystem system)
    {
        var house = TestAttributes.CompleteHouse with
        {
            HeatingSystem = system, HeatingEnergySource = null, HeatingDistribution = null,
        };

        Assert.Equal(new[] { "HeatingDistribution", "HeatingEnergySource" }, ErrorsFor(house).Order());
        Assert.Empty(ErrorsFor(house with
        {
            HeatingEnergySource = HeatingEnergySource.Electricity, HeatingDistribution = HeatingDistribution.Air,
        }));
    }

    [Theory]
    [InlineData(HeatingSystem.Convector)]
    [InlineData(HeatingSystem.InfraredPanels)]
    [InlineData(HeatingSystem.DistrictHeating)]
    [InlineData(HeatingSystem.Stove)]
    [InlineData(HeatingSystem.None)]
    public void House_HeatingWithoutItsOwnSource_ForbidsEnergySourceAndDistribution(HeatingSystem system)
    {
        var withDetails = TestAttributes.CompleteHouse with { HeatingSystem = system };

        Assert.Equal(new[] { "HeatingDistribution", "HeatingEnergySource" }, ErrorsFor(withDetails).Order());
        Assert.Empty(ErrorsFor(withDetails with { HeatingEnergySource = null, HeatingDistribution = null }));
    }

    [Fact]
    public void RequiresHeatingDetails_IsTrueOnlyForBoilerHeatPumpAndSolar()
    {
        var requiring = Enum.GetValues<HeatingSystem>().Where(s => HouseAttributes.RequiresHeatingDetails(s));

        Assert.Equal([HeatingSystem.OwnBoiler, HeatingSystem.HeatPump, HeatingSystem.SolarPanels], requiring);
        Assert.False(HouseAttributes.RequiresHeatingDetails(null));
    }

    [Fact]
    public void House_RejectsOutOfRangeNumbersAndUnknownAtticMaterial()
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
    public void Land_RequiresLandDesignationOnly()
    {
        Assert.Equal(new[] { "LandDesignation" }, ErrorsFor(new LandAttributes()));
        Assert.Empty(ErrorsFor(new LandAttributes(LandDesignation.Intravilan)));
    }

    [Fact]
    public void Commercial_RequiresSpaceType_AndCapsElectricalPowerLength()
    {
        Assert.Equal(new[] { "SpaceType" }, ErrorsFor(new CommercialAttributes()));
        Assert.Contains(
            "ElectricalPower",
            ErrorsFor(new CommercialAttributes(CommercialSpaceType.Office, ElectricalPower: new string('x', 51))));
    }

    [Fact]
    public void Garage_RequiresGarageType()
    {
        Assert.Equal(new[] { "GarageType" }, ErrorsFor(new GarageAttributes()));
        Assert.Empty(ErrorsFor(new GarageAttributes(GarageType.Individual)));
    }

    [Fact]
    public void Room_RequiresBathroomType_AndRejectsNegativeRoommates()
    {
        Assert.Equal(new[] { "PrivateOrSharedBathroom" }, ErrorsFor(new RoomAttributes()));
        Assert.Contains("RoommateCount", ErrorsFor(new RoomAttributes(BathroomType.Private, -1)));
    }
}
