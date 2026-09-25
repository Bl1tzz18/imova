using System.Text.Json;
using Imova.Application.Features.Listings.UpdateListing;
using Imova.Domain.Listings;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

// The bulk of the rules live in the shared ListingWriteValidator and are covered by
// CreateListingValidatorTests; this checks UpdateListingValidator actually inherits them, plus
// its own Id rule.
public class UpdateListingValidatorTests
{
    private readonly Guid _raionId;
    private readonly UpdateListingValidator _validator;

    public UpdateListingValidatorTests()
    {
        var dbContext = TestDbContextFactory.Create();
        var raion = Raion.Create(Guid.NewGuid(), "1000", "Chisinau", null, LocalityLabel.Sector);
        dbContext.Raioane.Add(raion);
        dbContext.SaveChanges();
        _raionId = raion.Id;
        _validator = new UpdateListingValidator(dbContext);
    }

    private UpdateListingCommand Command(Guid id, PropertyType propertyType, string attributesJson) =>
        new(
            id,
            Guid.NewGuid(),
            false,
            propertyType,
            54m,
            null,
            null,
            JsonDocument.Parse(attributesJson).RootElement.Clone(),
            null,
            null,
            "Moldova",
            _raionId,
            null,
            null,
            "Str. Ismail",
            null,
            TransactionType.Sale,
            "Titlu",
            "Descriere",
            50_000m,
            Currency.EUR,
            true,
            null);

    [Fact]
    public async Task Validate_WithValidCommand_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(Command(Guid.NewGuid(), PropertyType.Garage, """{"parkingType":"Garage"}"""));

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public async Task Validate_WithEmptyId_HasError()
    {
        var result = await _validator.ValidateAsync(Command(Guid.Empty, PropertyType.Garage, """{"parkingType":"Garage"}"""));

        Assert.Contains(result.Errors, e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task Validate_ChangingTypeWithoutMatchingAttributes_IsRejected()
    {
        // e.g. an Apartment edited into a Garage while the form still sends apartment fields.
        var result = await _validator.ValidateAsync(Command(Guid.NewGuid(), PropertyType.Garage, """{"rooms":2,"parkingType":"Garage"}"""));

        Assert.Contains(result.Errors, e => e.PropertyName == "TypeSpecificAttributes");
    }
}
