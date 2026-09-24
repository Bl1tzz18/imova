using Imova.Application.Features.Listings.Attributes;
using Imova.Domain.Properties.Attributes;

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
    public void House_RequiresRoomsAndHouseFloors_AndRejectsNonPositiveLandArea()
    {
        Assert.Equal(new[] { "HouseFloors", "Rooms" }, ErrorsFor(new HouseAttributes()).Distinct().Order());
        Assert.Contains("LandAreaM2", ErrorsFor(new HouseAttributes(Rooms: 3, HouseFloors: 1, LandAreaM2: 0)));
        Assert.Empty(ErrorsFor(new HouseAttributes(Rooms: 3, HouseFloors: 1)));
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
