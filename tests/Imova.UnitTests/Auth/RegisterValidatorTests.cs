using Imova.Application.Features.Auth.Register;

namespace Imova.UnitTests.Auth;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    private static RegisterCommand ValidCommand(
        string email = "user@example.com",
        string password = "SuperSecret1",
        string? displayName = "Test User",
        string phoneNumber = "+373 69 123 456") =>
        new(email, password, displayName, phoneNumber);

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyEmail_HasError()
    {
        var result = _validator.Validate(ValidCommand(email: ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Email));
    }

    [Fact]
    public void Validate_WithMalformedEmail_HasError()
    {
        var result = _validator.Validate(ValidCommand(email: "not-an-email"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Email));
    }

    [Fact]
    public void Validate_WithEmptyPassword_HasError()
    {
        var result = _validator.Validate(ValidCommand(password: ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public void Validate_WithPasswordShorterThanEightChars_HasError()
    {
        var result = _validator.Validate(ValidCommand(password: "Ab1defg"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public void Validate_WithNullDisplayName_HasNoErrors()
    {
        // DisplayName is optional at registration — see RegisterValidator.
        var result = _validator.Validate(ValidCommand(displayName: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithDisplayNameTooLong_HasError()
    {
        var result = _validator.Validate(ValidCommand(displayName: new string('a', 201)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.DisplayName));
    }

    [Fact]
    public void Validate_WithEmptyPhoneNumber_HasError()
    {
        // Unlike DisplayName, PhoneNumber is required at registration.
        var result = _validator.Validate(ValidCommand(phoneNumber: ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.PhoneNumber));
    }

    [Theory]
    [InlineData("abc123")]
    [InlineData("123")]
    [InlineData("+373-not-a-number")]
    public void Validate_WithInvalidPhoneNumber_HasError(string phoneNumber)
    {
        var result = _validator.Validate(ValidCommand(phoneNumber: phoneNumber));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.PhoneNumber));
    }

    [Theory]
    [InlineData("+373 69 123 456")]
    [InlineData("069123456")]
    [InlineData("+1 (555) 123-4567")]
    public void Validate_WithValidPhoneNumberFormats_HasNoErrors(string phoneNumber)
    {
        var result = _validator.Validate(ValidCommand(phoneNumber: phoneNumber));

        Assert.True(result.IsValid);
    }
}
