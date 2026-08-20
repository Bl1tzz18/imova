using FluentValidation;
using Imova.Application.Features.Properties.CreateProperty;
using Imova.Domain.Properties;

namespace Imova.UnitTests.Properties;

public class CreatePropertyValidatorTests
{
    private readonly CreatePropertyValidator _validator = new();

    private static CreatePropertyCommand ValidCommand(decimal price = 550m, string currency = "EUR") =>
        new(
            Guid.NewGuid(),
            "Apartament 2 camere",
            "Apartament luminos, aproape de centru.",
            PropertyType.Apartment,
            ListingType.Rent,
            price,
            currency,
            "Moldova",
            "Chisinau",
            "Botanica",
            47.0105,
            28.8638);

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
}
