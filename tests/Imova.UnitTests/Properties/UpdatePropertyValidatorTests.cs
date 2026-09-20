using Imova.Application.Features.Properties.UpdateProperty;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

// Mirrors CreatePropertyValidatorTests — UpdatePropertyValidator shares the same
// PropertyFieldRules-driven rules now that PropertyType/ListingType are editable too.
public class UpdatePropertyValidatorTests
{
    private readonly Guid _raionId;
    private readonly Guid _localitateId;
    private readonly Guid _otherRaionId;
    private readonly Guid _chisinauSectorId;
    private readonly UpdatePropertyValidator _validator;

    public UpdatePropertyValidatorTests()
    {
        var dbContext = TestDbContextFactory.Create();

        var raion = Raion.Create(Guid.NewGuid(), "1000", "Chisinau", null, LocalityLabel.Sector);
        var otherRaion = Raion.Create(Guid.NewGuid(), "1200", "Ialoveni", null, LocalityLabel.Localitate);
        var localitate = Localitate.Create(Guid.NewGuid(), raion.Id, null, "1001", "Botanica", null);
        var chisinauSector = ChisinauSector.Create("Botanica");
        dbContext.Raioane.AddRange(raion, otherRaion);
        dbContext.Localitati.Add(localitate);
        dbContext.ChisinauSectors.Add(chisinauSector);
        dbContext.SaveChanges();

        _raionId = raion.Id;
        _otherRaionId = otherRaion.Id;
        _localitateId = localitate.Id;
        _chisinauSectorId = chisinauSector.Id;
        _validator = new UpdatePropertyValidator(dbContext);
    }

    private UpdatePropertyCommand ValidCommand(
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
        bool? petsAllowed = null,
        string? streetAddress = null,
        Guid? raionId = null,
        Guid? localitateId = null,
        Guid? chisinauSectorId = null) =>
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
            raionId ?? _raionId,
            // Defaults to the seeded localitate only when the caller isn't testing
            // chisinauSectorId — otherwise an explicit `localitateId: null` would get silently
            // overridden back to a non-null value here, falsely triggering the new
            // LocalitateId/ChisinauSectorId mutual-exclusivity rule in unrelated tests.
            localitateId ?? (chisinauSectorId.HasValue ? null : _localitateId),
            chisinauSectorId,
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.RaionId));
    }

    [Fact]
    public async Task Validate_WithUnknownLocalitateId_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(localitateId: Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.LocalitateId));
    }

    [Fact]
    public async Task Validate_WithLocalitateBelongingToDifferentRaion_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(raionId: _otherRaionId, localitateId: _localitateId));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.LocalitateId));
    }

    [Fact]
    public async Task Validate_WithChisinauSector_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(ValidCommand(localitateId: null, chisinauSectorId: _chisinauSectorId));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithUnknownChisinauSectorId_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(localitateId: null, chisinauSectorId: Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.ChisinauSectorId));
    }

    [Fact]
    public async Task Validate_WithChisinauSectorOnNonChisinauRaion_HasError()
    {
        var result = await _validator.ValidateAsync(
            ValidCommand(raionId: _otherRaionId, localitateId: null, chisinauSectorId: _chisinauSectorId));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.ChisinauSectorId));
    }

    [Fact]
    public async Task Validate_WithChisinauSectorAndLocalitateBothSet_HasError()
    {
        // Mutually exclusive — a listing can be in a suburb or an informal Chișinău
        // neighborhood, never both at once (they're physically different places).
        var result = await _validator.ValidateAsync(
            ValidCommand(localitateId: _localitateId, chisinauSectorId: _chisinauSectorId));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.ChisinauSectorId));
    }

    [Fact]
    public async Task Validate_WithEmptyId_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(id: Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Id));
    }

    [Fact]
    public async Task Validate_WithEmptyTitle_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(title: ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Title));
    }

    [Fact]
    public async Task Validate_WithTitleTooLong_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(title: new string('a', 201)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Title));
    }

    [Fact]
    public async Task Validate_WithEmptyDescription_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(description: ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Description));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validate_WithNonPositivePrice_HasError(decimal price)
    {
        var result = await _validator.ValidateAsync(ValidCommand(price: price));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Price));
    }

    [Fact]
    public async Task Validate_WithCurrencyNotThreeLetters_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(currency: "EURO"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Currency));
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("MDL")]
    [InlineData("USD")]
    public async Task Validate_WithSupportedCurrency_HasNoCurrencyError(string currency)
    {
        var result = await _validator.ValidateAsync(ValidCommand(currency: currency));

        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Currency));
    }

    [Fact]
    public async Task Validate_WithUnsupportedCurrency_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(currency: "GBP"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Currency));
    }

    [Fact]
    public async Task Validate_ApartmentWithoutFloor_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(propertyType: PropertyType.Apartment, floor: null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Floor));
    }

    [Fact]
    public async Task Validate_HouseWithoutFloor_HasNoFloorError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(
            propertyType: PropertyType.House,
            listingType: ListingType.Sale,
            floor: null,
            totalFloors: 2));

        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Floor));
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Rooms));
    }

    [Fact]
    public async Task Validate_SalePropertyWithPetsAllowedSet_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(listingType: ListingType.Sale, petsAllowed: true));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.PetsAllowed));
    }

    [Fact]
    public async Task Validate_FloorGreaterThanTotalFloors_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(floor: 10, totalFloors: 5));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.Floor));
    }

    [Fact]
    public async Task Validate_WithStreetAddressOver200Chars_HasError()
    {
        var result = await _validator.ValidateAsync(ValidCommand(streetAddress: new string('a', 201)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePropertyCommand.StreetAddress));
    }
}
