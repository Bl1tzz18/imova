using Imova.Application.Features.Properties.UpdateProperty;
using Imova.Domain.Properties;

namespace Imova.UnitTests.Properties;

// Mirrors CreatePropertyValidatorTests — UpdatePropertyValidator shares the same
// PropertyFieldRules-driven rules now that PropertyType/ListingType are editable too.
public class UpdatePropertyValidatorTests
{
    private readonly UpdatePropertyValidator _validator = new();

    private static UpdatePropertyCommand ValidCommand(
        Guid? id = null,
        string title = "Titlu",
        string description = "Descriere",
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
            id ?? Guid.NewGuid(),
            Guid.NewGuid(),
            false,
            title,
            description,
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
    public void Validate_WithEmptyId_HasError()
    {
        var result = _validator.Validate(ValidCommand(id: Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Id));
    }

    [Fact]
    public void Validate_WithEmptyTitle_HasError()
    {
        var result = _validator.Validate(ValidCommand(title: ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Title));
    }

    [Fact]
    public void Validate_WithTitleTooLong_HasError()
    {
        var result = _validator.Validate(ValidCommand(title: new string('a', 201)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Title));
    }

    [Fact]
    public void Validate_WithEmptyDescription_HasError()
    {
        var result = _validator.Validate(ValidCommand(description: ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Description));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositivePrice_HasError(decimal price)
    {
        var result = _validator.Validate(ValidCommand(price: price));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Price));
    }

    [Fact]
    public void Validate_WithCurrencyNotThreeLetters_HasError()
    {
        var result = _validator.Validate(ValidCommand(currency: "EURO"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Currency));
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("MDL")]
    [InlineData("USD")]
    public void Validate_WithSupportedCurrency_HasNoCurrencyError(string currency)
    {
        var result = _validator.Validate(ValidCommand(currency: currency));

        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Currency));
    }

    [Fact]
    public void Validate_WithUnsupportedCurrency_HasError()
    {
        var result = _validator.Validate(ValidCommand(currency: "GBP"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Currency));
    }

    [Fact]
    public void Validate_ApartmentWithoutFloor_HasError()
    {
        var result = _validator.Validate(ValidCommand(propertyType: PropertyType.Apartment, floor: null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Floor));
    }

    [Fact]
    public void Validate_HouseWithoutFloor_HasNoFloorError()
    {
        var result = _validator.Validate(ValidCommand(
            propertyType: PropertyType.House,
            listingType: ListingType.Sale,
            floor: null,
            totalFloors: 2));

        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Floor));
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Rooms));
    }

    [Fact]
    public void Validate_SalePropertyWithPetsAllowedSet_HasError()
    {
        var result = _validator.Validate(ValidCommand(listingType: ListingType.Sale, petsAllowed: true));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.PetsAllowed));
    }

    [Fact]
    public void Validate_FloorGreaterThanTotalFloors_HasError()
    {
        var result = _validator.Validate(ValidCommand(floor: 10, totalFloors: 5));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Floor));
    }
}
