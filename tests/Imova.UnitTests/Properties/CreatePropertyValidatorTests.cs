using Imova.Application.Features.Properties.CreateProperty;
using Imova.Domain.Properties;

namespace Imova.UnitTests.Properties;

public class CreatePropertyValidatorTests
{
    private readonly CreatePropertyValidator _validator = new();

    private static CreatePropertyCommand ValidCommand(
        decimal price = 550m,
        string currency = "EUR",
        PropertyType propertyType = PropertyType.Apartment,
        ListingType listingType = ListingType.Rent,
        decimal? area = 54m,
        decimal? rooms = 2m,
        short? bathrooms = null,
        short? floor = 3,
        short? totalFloors = 9,
        short? yearBuilt = null,
        bool? furnished = null,
        bool? parkingAvailable = null,
        bool? petsAllowed = null) =>
        new(
            null,
            Guid.NewGuid(),
            "Apartament 2 camere",
            "Apartament luminos, aproape de centru.",
            propertyType,
            listingType,
            price,
            currency,
            "Moldova",
            "Chisinau",
            "Botanica",
            47.0105,
            28.8638,
            area,
            rooms,
            bathrooms,
            floor,
            totalFloors,
            yearBuilt,
            furnished,
            parkingAvailable,
            petsAllowed);

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithNonPositivePrice_HasError()
    {
        var result = _validator.Validate(ValidCommand(price: 0m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Price));
    }

    [Fact]
    public void Validate_WithCurrencyNotThreeLetters_HasError()
    {
        var result = _validator.Validate(ValidCommand(currency: "EURO"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Currency));
    }

    [Fact]
    public void Validate_ApartmentWithoutFloor_HasError()
    {
        var result = _validator.Validate(ValidCommand(propertyType: PropertyType.Apartment, floor: null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Floor));
    }

    [Fact]
    public void Validate_HouseWithoutFloor_HasNoFloorError()
    {
        // A house isn't "on" a floor of a bigger building, so Floor doesn't apply — only
        // TotalFloors (how many floors the house itself has) is required.
        var result = _validator.Validate(ValidCommand(
            propertyType: PropertyType.House,
            listingType: ListingType.Sale,
            floor: null,
            totalFloors: 2));

        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Floor));
    }

    [Fact]
    public void Validate_HouseWithFloorSet_HasError()
    {
        var result = _validator.Validate(ValidCommand(
            propertyType: PropertyType.House,
            listingType: ListingType.Sale,
            floor: 1,
            totalFloors: 2));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Floor));
    }

    [Fact]
    public void Validate_LandWithAreaOnly_HasNoErrors()
    {
        var result = _validator.Validate(ValidCommand(
            propertyType: PropertyType.Land,
            listingType: ListingType.Sale,
            area: 600m,
            rooms: null,
            floor: null,
            totalFloors: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_LandWithRoomsSet_HasError()
    {
        var result = _validator.Validate(ValidCommand(
            propertyType: PropertyType.Land,
            listingType: ListingType.Sale,
            area: 600m,
            rooms: 1m,
            floor: null,
            totalFloors: null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Rooms));
    }

    [Fact]
    public void Validate_SalePropertyWithPetsAllowedSet_HasError()
    {
        // PetsAllowed only makes sense for a rental.
        var result = _validator.Validate(ValidCommand(listingType: ListingType.Sale, petsAllowed: true));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.PetsAllowed));
    }

    [Fact]
    public void Validate_FloorGreaterThanTotalFloors_HasError()
    {
        var result = _validator.Validate(ValidCommand(floor: 10, totalFloors: 5));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Floor));
    }
}
