using Imova.Application.Features.Auth.Login;

namespace Imova.UnitTests.Auth;

public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new LoginCommand("user@example.com", "SuperSecret1"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyEmail_HasError()
    {
        var result = _validator.Validate(new LoginCommand("", "SuperSecret1"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCommand.Email));
    }

    [Fact]
    public void Validate_WithEmptyPassword_HasError()
    {
        var result = _validator.Validate(new LoginCommand("user@example.com", ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCommand.Password));
    }

    [Fact]
    public void Validate_WithEmailTooLong_HasError()
    {
        var longEmail = new string('a', 250) + "@example.com";

        var result = _validator.Validate(new LoginCommand(longEmail, "SuperSecret1"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCommand.Email));
    }

    [Fact]
    public void Validate_DoesNotEnforceEmailFormat()
    {
        // Unlike RegisterValidator, LoginValidator only checks NotEmpty for Email — format
        // validation isn't its job (a malformed email will simply fail to match any account).
        var result = _validator.Validate(new LoginCommand("not-an-email", "SuperSecret1"));

        Assert.True(result.IsValid);
    }
}
