using System.Text.Json;
using Imova.Application.Features.Listings.Attributes;
using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class PropertyAttributesJsonTests
{
    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public void TryParse_ApartmentFields_ProducesTypedApartmentAttributes()
    {
        var ok = PropertyAttributesJson.TryParse(
            PropertyType.Apartment,
            Json("""{"rooms":2,"floor":3,"totalFloors":9,"bathrooms":1,"heatingSystem":"DistrictHeating","layout":"Studio"}"""),
            out var attributes,
            out var error);

        Assert.True(ok, error);
        Assert.Equal(
            new ApartmentAttributes(Rooms: 2, Floor: 3, TotalFloors: 9, Bathrooms: 1, HeatingSystem: HeatingSystem.DistrictHeating, Layout: ApartmentLayout.Studio),
            attributes);
    }

    [Fact]
    public void TryParse_HouseDetails_ParsesEnumsDecimalsAndBooleans()
    {
        var ok = PropertyAttributesJson.TryParse(
            PropertyType.House,
            Json("""{"rooms":4,"houseType":"Duplex","buildingMaterial":"LimestoneBlock","livingAreaM2":140.5,"heatingSystem":"OwnBoiler","heatingEnergySource":"Gas","gasSupply":true,"atticMaterial":"Drywall"}"""),
            out var attributes,
            out _);

        Assert.True(ok);
        var house = Assert.IsType<HouseAttributes>(attributes);
        Assert.Equal(HouseType.Duplex, house.HouseType);
        Assert.Equal(BuildingMaterial.LimestoneBlock, house.BuildingMaterial);
        Assert.Equal(140.5m, house.LivingAreaM2);
        Assert.Equal(HeatingEnergySource.Gas, house.HeatingEnergySource);
        Assert.True(house.GasSupply);
        Assert.Equal(AtticMaterial.Drywall, house.AtticMaterial);
    }

    [Fact]
    public void TryParse_HouseWithTheRetiredUtilitiesObject_IsRejected()
    {
        var ok = PropertyAttributesJson.TryParse(
            PropertyType.House, Json("""{"utilities":{"water":true}}"""), out _, out var error);

        Assert.False(ok);
        Assert.Equal("'utilities' is not a field of PropertyType House.", error);
    }

    [Fact]
    public void TryParse_IsCaseInsensitiveForKeysAndEnumNames()
    {
        var ok = PropertyAttributesJson.TryParse(
            PropertyType.Garage, Json("""{"ParkingType":"undergroundParking"}"""), out var attributes, out _);

        Assert.True(ok);
        Assert.Equal(new GarageAttributes(ParkingType.UndergroundParking), attributes);
    }

    [Theory]
    [InlineData(PropertyType.Land, """{"rooms":2}""", "'rooms' is not a field of PropertyType Land.")]
    [InlineData(PropertyType.Garage, """{"floor":1,"parkingType":"Garage"}""", "'floor' is not a field of PropertyType Garage.")]
    [InlineData(PropertyType.Apartment, """{"rooms":2,"plotType":"Forest"}""", "'plotType' is not a field of PropertyType Apartment.")]
    [InlineData(PropertyType.Apartment, """{"heatingType":"Autonomous"}""", "'heatingType' is not a field of PropertyType Apartment.")]
    public void TryParse_WithAFieldFromAnotherTypesSchema_IsRejectedByName(PropertyType type, string json, string expectedError)
    {
        var ok = PropertyAttributesJson.TryParse(type, Json(json), out var attributes, out var error);

        Assert.False(ok);
        Assert.Null(attributes);
        Assert.Equal(expectedError, error);
    }

    [Fact]
    public void TryParse_WithUnknownNestedField_IsRejected()
    {
        var ok = PropertyAttributesJson.TryParse(
            PropertyType.Land, Json("""{"plotType":{"nested":true}}"""), out _, out var error);

        Assert.False(ok);
        Assert.StartsWith("TypeSpecificAttributes is malformed", error);
    }

    [Theory]
    [InlineData("""{"rooms":"two"}""")]
    [InlineData("""{"heatingSystem":"Solar"}""")]
    public void TryParse_WithWrongValueTypes_IsRejectedAsMalformed(string json)
    {
        var ok = PropertyAttributesJson.TryParse(PropertyType.Apartment, Json(json), out _, out var error);

        Assert.False(ok);
        Assert.StartsWith("TypeSpecificAttributes is malformed", error);
    }

    [Fact]
    public void TryParse_WithNonObjectJson_IsRejected()
    {
        var ok = PropertyAttributesJson.TryParse(PropertyType.Apartment, Json("[1,2]"), out _, out var error);

        Assert.False(ok);
        Assert.Equal("TypeSpecificAttributes must be a JSON object.", error);
    }

    [Fact]
    public void TryParse_WithNoJson_ReturnsTheTypesEmptyAttributes()
    {
        var ok = PropertyAttributesJson.TryParse(PropertyType.Room, null, out var attributes, out _);

        Assert.True(ok);
        Assert.Equal(new RoomAttributes(), attributes);
    }

    [Fact]
    public void FieldNamesFor_ListsExactlyTheTypesSchemaInCamelCase()
    {
        Assert.Equal(
            new[]
            {
                "bathrooms", "electricalPower", "finishCondition", "floor", "gasSupply", "mainStreetAccess",
                "numberOfOffices", "phoneLinesCount", "spaceType", "totalFloorsInBuilding", "workingAreaM2",
            },
            PropertyAttributesJson.FieldNamesFor(PropertyType.Commercial).Order());
    }

    [Fact]
    public void Storage_RoundTripsEveryTypeThroughTheKindDiscriminator()
    {
        PropertyAttributes[] all =
        [
            TestAttributes.CompleteApartment,
            TestAttributes.CompleteHouse,
            TestAttributes.CompleteLand,
            TestAttributes.CompleteCommercial,
            TestAttributes.CompleteGarage,
            TestAttributes.CompleteRoom,
        ];

        foreach (var attributes in all)
        {
            var json = PropertyAttributesJson.ToStorageJson(attributes);

            Assert.Contains($"\"kind\":\"{attributes.GetPropertyType()}\"", json);
            Assert.Equal(attributes, PropertyAttributesJson.FromStorageJson(json));
        }
    }

    [Fact]
    public void Storage_ReadsTheDiscriminatorEvenWhenItIsNotTheFirstKey()
    {
        // Postgres' jsonb reorders keys, so the stored "kind" can come after the fields.
        var attributes = PropertyAttributesJson.FromStorageJson("""{"floor": 2, "rooms": 2, "kind": "Apartment", "totalFloors": 5}""");

        Assert.Equal(new ApartmentAttributes(Rooms: 2, Floor: 2, TotalFloors: 5), attributes);
    }

    [Fact]
    public void ToWireElement_UsesCamelCaseStringEnumsAndNoDiscriminator()
    {
        var element = PropertyAttributesJson.ToWireElement(new GarageAttributes(ParkingType.UndergroundParking));

        Assert.Equal("UndergroundParking", element.GetProperty("parkingType").GetString());
        Assert.False(element.TryGetProperty("kind", out _));
    }
}
