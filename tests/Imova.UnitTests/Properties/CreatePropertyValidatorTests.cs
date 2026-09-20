using Imova.Application.Features.Properties.CreateProperty;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class CreatePropertyValidatorTests
{
    private readonly Guid _raionId;
    private readonly Guid _localitateId;
    private readonly Guid _otherRaionId;
    private readonly CreatePropertyValidator _validator;

    public CreatePropertyValidatorTests()
    {
        var dbContext = TestDbContextFactory.Create();

        var raion = Raion.Create(Guid.NewGuid(), "1000", "Chisinau", null, LocalityLabel.Sector);
        var otherRaion = Raion.Create(Guid.NewGuid(), "1200", "Ialoveni", null, LocalityLabel.Localitate);
        var localitate = Localitate.Create(Guid.NewGuid(), raion.Id, null, "1001", "Botanica", null);
        dbContext.Raioane.AddRange(raion, otherRaion);
        dbContext.Localitati.Add(localitate);
        dbContext.SaveChanges();

        _raionId = raion.Id;
        _otherRaionId = otherRaion.Id;
        _localitateId = localitate.Id;
        _validator = new CreatePropertyValidator(dbContext);
    }

    private CreatePropertyCommand ValidCommand(
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
        bool? petsAllowed = null,
        string? streetAddress = null,
        Guid? raionId = null,
        Guid? localitateId = null) =>
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
            raionId ?? _raionId,
            localitateId ?? _localitateId,
            streetAddress,
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
    public async Task Validate_WithValidCommand_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithoutLocalitate_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(ValidCommand(localitateId: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithUnknownRaionId_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(raionId: Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.RaionId));
    }

    [Fact]
    public async Task Validate_WithUnknownLocalitateId_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(localitateId: Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.LocalitateId));
    }

    [Fact]
    public async Task Validate_WithLocalitateBelongingToDifferentRaion_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(raionId: _otherRaionId, localitateId: _localitateId));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.LocalitateId));
    }

    [Fact]
    public async Task Validate_WithNonPositivePrice_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(price: 0m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Price));
    }

    [Fact]
    public async Task Validate_WithCurrencyNotThreeLetters_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(currency: "EURO"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Currency));
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("MDL")]
    [InlineData("USD")]
    public async Task Validate_WithSupportedCurrency_HasNoCurrencyError(string currency)
    {
        var result = await _validator.ValidateAsync(ValidCommand(currency: currency));

        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Currency));
    }

    [Fact]
    public async Task Validate_WithUnsupportedCurrency_HasError()
    {
        // Only EUR/MDL/USD are offered on the frontend dropdown — a well-formed but unsupported
        // 3-letter code must still fail.
        var result = await _validator.ValidateAsync(ValidCommand(currency: "GBP"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Currency));
    }

    [Fact]
    public async Task Validate_ApartmentWithoutFloor_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(propertyType: PropertyType.Apartment, floor: null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Floor));
    }

    [Fact]
    public async Task Validate_HouseWithoutFloor_HasNoFloorError()
    {
        // A house isn't "on" a floor of a bigger building, so Floor doesn't apply — only
        // TotalFloors (how many floors the house itself has) is required.
        var result = await _validator.ValidateAsync(ValidCommand(
            propertyType: PropertyType.House,
            listingType: ListingType.Sale,
            floor: null,
            totalFloors: 2));

        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Floor));
    }

    [Fact]
    public async Task Validate_HouseWithFloorSet_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(
            propertyType: PropertyType.House,
            listingType: ListingType.Sale,
            floor: 1,
            totalFloors: 2));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Floor));
    }

    [Fact]
    public async Task Validate_LandWithAreaOnly_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(ValidCommand(
            propertyType: PropertyType.Land,
            listingType: ListingType.Sale,
            area: 600m,
            rooms: null,
            floor: null,
            totalFloors: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_LandWithRoomsSet_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(
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
    public async Task Validate_SalePropertyWithPetsAllowedSet_HasError()
    {
        // PetsAllowed only makes sense for a rental.
        var result = await _validator.ValidateAsync(ValidCommand(listingType: ListingType.Sale, petsAllowed: true));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.PetsAllowed));
    }

    [Fact]
    public async Task Validate_FloorGreaterThanTotalFloors_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(floor: 10, totalFloors: 5));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Floor));
    }

    [Fact]
    public async Task Validate_WithStreetAddressOver200Chars_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(streetAddress: new string('a', 201)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.StreetAddress));
    }

    [Fact]
    public async Task Validate_WithoutStreetAddress_HasNoError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(streetAddress: null));

        Assert.True(result.IsValid);
    }
}
