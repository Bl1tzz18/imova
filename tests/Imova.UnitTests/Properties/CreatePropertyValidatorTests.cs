using FluentValidation;
using Imova.Application.Features.Properties.CreateProperty;

namespace Imova.UnitTests.Properties;

public class CreatePropertyValidatorTests
{
    private readonly CreatePropertyValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        var command = new CreatePropertyCommand("Apartament 2 camere", 550m, "EUR", "Chisinau", "Botanica");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithNonPositivePrice_HasError()
    {
        var command = new CreatePropertyCommand("Apartament 2 camere", 0m, "EUR", "Chisinau", "Botanica");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Price));
    }

    [Fact]
    public void Validate_WithCurrencyNotThreeLetters_HasError()
    {
        var command = new CreatePropertyCommand("Apartament 2 camere", 550m, "EURO", "Chisinau", "Botanica");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePropertyCommand.Currency));
    }
}
