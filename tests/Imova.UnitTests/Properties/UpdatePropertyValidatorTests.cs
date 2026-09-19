using Imova.Application.Features.Properties.UpdateProperty;

namespace Imova.UnitTests.Properties;

public class UpdatePropertyValidatorTests
{
    private readonly UpdatePropertyValidator _validator = new();

    private static UpdatePropertyCommand ValidCommand(
        Guid? id = null,
        string title = "Titlu",
        string description = "Descriere",
        decimal price = 550m) =>
        new(id ?? Guid.NewGuid(), Guid.NewGuid(), false, title, description, price);

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
}
