using System.Text.Json;
using Imova.Application.Features.Listings.CreateListing;
using Imova.Domain.Listings;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class CreateListingValidatorTests
{
    private readonly Guid _raionId;
    private readonly Guid _localitateId;
    private readonly Guid _otherRaionId;
    private readonly Guid _chisinauSectorId;
    private readonly Guid _amenityId;
    private readonly Guid _furnishedAmenityId;
    private readonly Guid _saunaAmenityId;
    private readonly CreateListingValidator _validator;

    public CreateListingValidatorTests()
    {
        var dbContext = TestDbContextFactory.Create();

        var raion = Raion.Create(Guid.NewGuid(), "1000", "Chisinau", null, LocalityLabel.Sector);
        var otherRaion = Raion.Create(Guid.NewGuid(), "1200", "Ialoveni", null, LocalityLabel.Localitate);
        var localitate = Localitate.Create(Guid.NewGuid(), raion.Id, null, "1001", "Botanica", null);
        var chisinauSector = ChisinauSector.Create("Botanica");
        var amenity = new Imova.Domain.Amenities.Amenity(Guid.NewGuid(), "parking", "Parcare");
        dbContext.Raioane.AddRange(raion, otherRaion);
        dbContext.Localitati.Add(localitate);
        dbContext.ChisinauSectors.Add(chisinauSector);
        var furnished = new Imova.Domain.Amenities.Amenity(Guid.NewGuid(), "furnished", "Mobilat", Imova.Domain.Amenities.AmenityCategory.Comfort);
        var sauna = new Imova.Domain.Amenities.Amenity(
            Guid.NewGuid(), "sauna", "Saună", Imova.Domain.Amenities.AmenityCategory.Leisure, [PropertyType.House]);
        dbContext.Amenities.AddRange(amenity, furnished, sauna);
        dbContext.SaveChanges();

        _raionId = raion.Id;
        _otherRaionId = otherRaion.Id;
        _localitateId = localitate.Id;
        _chisinauSectorId = chisinauSector.Id;
        _amenityId = amenity.Id;
        _furnishedAmenityId = furnished.Id;
        _saunaAmenityId = sauna.Id;
        _validator = new CreateListingValidator(dbContext);
    }

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static JsonElement Attrs(Imova.Domain.Properties.Attributes.PropertyAttributes attributes) =>
        JsonSerializer.SerializeToElement(attributes, attributes.GetType(), Imova.Application.Features.Listings.Attributes.PropertyAttributesJson.WireOptions);

    private static readonly JsonElement ApartmentJson =
        JsonSerializer.SerializeToElement(TestAttributes.CompleteApartment, Imova.Application.Features.Listings.Attributes.PropertyAttributesJson.WireOptions);

    private CreateListingCommand ValidCommand(
        PropertyType propertyType = PropertyType.Apartment,
        JsonElement? attributes = null,
        bool useDefaultAttributes = true,
        decimal totalAreaM2 = 54m,
        int? yearBuilt = null,
        PropertyCondition? condition = null,
        IReadOnlyList<Guid>? amenityIds = null,
        TransactionType transactionType = TransactionType.Rent,
        decimal price = 550m,
        Currency currency = Currency.EUR,
        RentalDetails? rentalDetails = null,
        string? streetAddress = "Str. Ismail",
        string? buildingNumber = null,
        Guid? raionId = null,
        Guid? localitateId = null,
        Guid? chisinauSectorId = null,
        Guid? publisherId = null) =>
        new(
            null,
            Guid.NewGuid(),
            publisherId,
            propertyType,
            totalAreaM2,
            yearBuilt,
            condition,
            attributes ?? (useDefaultAttributes ? ApartmentJson : null),
            amenityIds,
            "Moldova",
            raionId ?? _raionId,
            // Defaults to the seeded localitate only when the caller isn't testing
            // chisinauSectorId, which is mutually exclusive with it.
            localitateId ?? (chisinauSectorId.HasValue ? null : _localitateId),
            chisinauSectorId,
            streetAddress,
            buildingNumber,
            transactionType,
            "Apartament 2 camere",
            "Apartament luminos, aproape de centru.",
            price,
            currency,
            false,
            rentalDetails);

    private async Task<List<string>> ErrorPropertiesAsync(CreateListingCommand command) =>
        (await _validator.ValidateAsync(command)).Errors.Select(e => e.PropertyName).ToList();

    [Fact]
    public async Task Validate_WithValidCommand_HasNoErrors()
    {
        Assert.Empty(await ErrorPropertiesAsync(ValidCommand()));
    }

    // --- TypeSpecificAttributes, conditioned on PropertyType ---

    [Fact]
    public async Task Validate_LandWithApartmentFields_IsRejected()
    {
        var result = await _validator.ValidateAsync(ValidCommand(
            propertyType: PropertyType.Land, attributes: Json("""{"plotType":"Forest","rooms":2}""")));

        var error = Assert.Single(result.Errors);
        Assert.Equal("TypeSpecificAttributes", error.PropertyName);
        Assert.Equal("'rooms' is not a field of PropertyType Land.", error.ErrorMessage);
    }

    [Fact]
    public async Task Validate_LandWithItsOwnFields_HasNoErrors()
    {
        Assert.Empty(await ErrorPropertiesAsync(ValidCommand(
            propertyType: PropertyType.Land,
            attributes: Attrs(TestAttributes.CompleteLand))));
    }

    [Fact]
    public async Task Validate_ApartmentMissingRequiredAttributes_ReportsEachUnderTypeSpecificAttributes()
    {
        var errors = await ErrorPropertiesAsync(ValidCommand(attributes: Json("""{"rooms":2}""")));

        Assert.Contains("TypeSpecificAttributes.Floor", errors);
        Assert.Contains("TypeSpecificAttributes.HeatingSystem", errors);
        Assert.DoesNotContain("TypeSpecificAttributes.Rooms", errors);
    }

    [Fact]
    public async Task Validate_WithoutAttributesAtAll_StillRequiresTheTypesRequiredFields()
    {
        var errors = await ErrorPropertiesAsync(ValidCommand(propertyType: PropertyType.Garage, useDefaultAttributes: false));

        Assert.Equal(["TypeSpecificAttributes.ParkingType"], errors);
    }

    [Fact]
    public async Task Validate_WithUndefinedPropertyType_ReportsOnlyThePropertyTypeNotTheAttributes()
    {
        var errors = await ErrorPropertiesAsync(ValidCommand(propertyType: (PropertyType)42));

        Assert.Equal(["PropertyType"], errors);
    }

    // --- Property fields ---

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validate_WithNonPositiveArea_HasError(decimal area)
    {
        Assert.Contains("TotalAreaM2", await ErrorPropertiesAsync(ValidCommand(totalAreaM2: area)));
    }

    [Fact]
    public async Task Validate_LandWithYearBuiltOrCondition_HasErrors()
    {
        var errors = await ErrorPropertiesAsync(ValidCommand(
            propertyType: PropertyType.Land,
            attributes: Attrs(TestAttributes.CompleteLand),
            yearBuilt: 2000,
            condition: PropertyCondition.New));

        Assert.Contains("YearBuilt", errors);
        Assert.Contains("Condition", errors);
    }

    [Fact]
    public async Task Validate_WithYearBuiltOutOfRange_HasError()
    {
        Assert.Contains("YearBuilt", await ErrorPropertiesAsync(ValidCommand(yearBuilt: 1700)));
    }

    [Fact]
    public async Task Validate_WithKnownAmenities_HasNoErrors()
    {
        Assert.Empty(await ErrorPropertiesAsync(ValidCommand(amenityIds: [_amenityId, _amenityId])));
    }

    [Fact]
    public async Task Validate_WithUnknownAmenity_HasError()
    {
        Assert.Contains("AmenityIds", await ErrorPropertiesAsync(ValidCommand(amenityIds: [_amenityId, Guid.NewGuid()])));
    }

    [Fact]
    public async Task Validate_FurnishedAmenityOnARental_IsRejected()
    {
        var result = await _validator.ValidateAsync(ValidCommand(amenityIds: [_amenityId, _furnishedAmenityId]));

        Assert.Contains(result.Errors, e =>
            e.PropertyName == "AmenityIds" && e.ErrorMessage.Contains("RentalDetails.FurnishedStatus"));
    }

    [Fact]
    public async Task Validate_FurnishedAmenityOnASale_IsAllowed()
    {
        Assert.Empty(await ErrorPropertiesAsync(ValidCommand(
            transactionType: TransactionType.Sale, amenityIds: [_furnishedAmenityId])));
    }

    public static TheoryData<PropertyType> FinishConditionTypes() => [PropertyType.House, PropertyType.Apartment, PropertyType.Commercial];

    private static Imova.Domain.Properties.Attributes.PropertyAttributes CompleteFor(PropertyType type) => type switch
    {
        PropertyType.Apartment => TestAttributes.CompleteApartment,
        PropertyType.House => TestAttributes.CompleteHouse,
        PropertyType.Land => TestAttributes.CompleteLand,
        PropertyType.Commercial => TestAttributes.CompleteCommercial,
        PropertyType.Garage => TestAttributes.CompleteGarage,
        _ => TestAttributes.CompleteRoom,
    };

    [Theory]
    [MemberData(nameof(FinishConditionTypes))]
    public async Task Validate_TypesUsingFinishCondition_RejectTheGeneralCondition(PropertyType type)
    {
        var errors = await ErrorPropertiesAsync(ValidCommand(
            propertyType: type, attributes: Attrs(CompleteFor(type)), condition: PropertyCondition.New));

        Assert.Equal(["Condition"], errors);
    }

    [Theory]
    [InlineData(PropertyType.Garage)]
    [InlineData(PropertyType.Room)]
    public async Task Validate_GarageAndRoom_StillAcceptTheGeneralCondition(PropertyType type)
    {
        Assert.Empty(await ErrorPropertiesAsync(ValidCommand(
            propertyType: type, attributes: Attrs(CompleteFor(type)), condition: PropertyCondition.Renovated)));
    }

    [Theory]
    [InlineData(PropertyType.Apartment)]
    [InlineData(PropertyType.House)]
    [InlineData(PropertyType.Land)]
    [InlineData(PropertyType.Commercial)]
    [InlineData(PropertyType.Garage)]
    [InlineData(PropertyType.Room)]
    public async Task Validate_CompleteListingOfEachType_HasNoErrors(PropertyType type)
    {
        Assert.Empty(await ErrorPropertiesAsync(ValidCommand(
            propertyType: type,
            attributes: Attrs(CompleteFor(type)),
            yearBuilt: type == PropertyType.Land ? null : 2010)));
    }

    [Fact]
    public async Task Validate_AmenityThatDoesNotApplyToThePropertyType_IsRejected()
    {
        var errors = (await _validator.ValidateAsync(ValidCommand(
            propertyType: PropertyType.Garage,
            attributes: Attrs(TestAttributes.CompleteGarage),
            transactionType: TransactionType.Sale,
            amenityIds: [_saunaAmenityId]))).Errors;

        Assert.Contains(errors, e => e.PropertyName == "AmenityIds" && e.ErrorMessage == "The 'sauna' amenity doesn't apply to a Garage.");
    }

    [Fact]
    public async Task Validate_CompleteHouse_HasNoErrors()
    {
        Assert.Empty(await ErrorPropertiesAsync(ValidCommand(
            propertyType: PropertyType.House,
            attributes: JsonSerializer.SerializeToElement(TestAttributes.CompleteHouse, Imova.Application.Features.Listings.Attributes.PropertyAttributesJson.WireOptions))));
    }

    // --- Listing fields ---

    [Fact]
    public async Task Validate_SaleWithRentalDetails_HasError()
    {
        var errors = await ErrorPropertiesAsync(ValidCommand(
            transactionType: TransactionType.Sale, rentalDetails: new RentalDetails()));

        Assert.Contains("RentalDetails", errors);
    }

    [Fact]
    public async Task Validate_RentWithoutRentalDetails_HasNoErrors()
    {
        // Optional for a rental — CreateListingHandler defaults it.
        Assert.Empty(await ErrorPropertiesAsync(ValidCommand(rentalDetails: null)));
    }

    [Fact]
    public async Task Validate_RentWithInvalidRentalTerms_ReportsNestedFields()
    {
        var errors = await ErrorPropertiesAsync(ValidCommand(rentalDetails: new RentalDetails(
            MinLeasePeriodMonths: 0, SecurityDepositAmount: -5, FurnishedStatus: (FurnishedStatus)9)));

        Assert.Contains("RentalDetails.MinLeasePeriodMonths", errors);
        Assert.Contains("RentalDetails.SecurityDepositAmount", errors);
        Assert.Contains("RentalDetails.FurnishedStatus", errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task Validate_WithNonPositivePrice_HasError(decimal price)
    {
        Assert.Contains("Price", await ErrorPropertiesAsync(ValidCommand(price: price)));
    }

    [Theory]
    [InlineData(Currency.EUR)]
    [InlineData(Currency.MDL)]
    [InlineData(Currency.USD)]
    public async Task Validate_WithSupportedCurrency_HasNoErrors(Currency currency)
    {
        Assert.Empty(await ErrorPropertiesAsync(ValidCommand(currency: currency)));
    }

    [Fact]
    public async Task Validate_WithUndefinedCurrency_HasError()
    {
        Assert.Contains("Currency", await ErrorPropertiesAsync(ValidCommand(currency: (Currency)7)));
    }

    [Fact]
    public async Task Validate_WithEmptyPublisherId_HasError()
    {
        Assert.Contains("PublisherId", await ErrorPropertiesAsync(ValidCommand(publisherId: Guid.Empty)));
    }

    // --- Location (unchanged rules, carried over from the old CreatePropertyValidator) ---

    [Fact]
    public async Task Validate_WithUnknownRaionId_HasError()
    {
        Assert.Contains("RaionId", await ErrorPropertiesAsync(ValidCommand(raionId: Guid.NewGuid(), localitateId: null)));
    }

    [Fact]
    public async Task Validate_WithLocalitateBelongingToDifferentRaion_HasError()
    {
        Assert.Contains("LocalitateId", await ErrorPropertiesAsync(ValidCommand(raionId: _otherRaionId, localitateId: _localitateId)));
    }

    [Fact]
    public async Task Validate_WithChisinauSector_HasNoErrors()
    {
        Assert.Empty(await ErrorPropertiesAsync(ValidCommand(chisinauSectorId: _chisinauSectorId)));
    }

    [Fact]
    public async Task Validate_WithChisinauSectorOnNonChisinauRaion_HasError()
    {
        Assert.Contains(
            "ChisinauSectorId",
            await ErrorPropertiesAsync(ValidCommand(raionId: _otherRaionId, chisinauSectorId: _chisinauSectorId)));
    }

    [Fact]
    public async Task Validate_WithChisinauSectorAndLocalitateBothSet_HasError()
    {
        Assert.Contains(
            "ChisinauSectorId",
            await ErrorPropertiesAsync(ValidCommand(localitateId: _localitateId, chisinauSectorId: _chisinauSectorId)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithoutStreetAddress_HasError(string? streetAddress)
    {
        Assert.Contains("StreetAddress", await ErrorPropertiesAsync(ValidCommand(streetAddress: streetAddress)));
    }

    [Fact]
    public async Task Validate_WithBuildingNumberOver20Chars_HasError()
    {
        Assert.Contains("BuildingNumber", await ErrorPropertiesAsync(ValidCommand(buildingNumber: new string('1', 21))));
    }
}
