using Imova.Application.Features.Auth.GoogleLogin;

namespace Imova.UnitTests.Auth;

public class GoogleLoginValidatorTests
{
    private readonly GoogleLoginValidator _validator = new();

    [Fact]
    public void Validate_WithIdToken_HasNoErrors()
    {
        var result = _validator.Validate(new GoogleLoginCommand("some-id-token"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyIdToken_HasError()
    {
        var result = _validator.Validate(new GoogleLoginCommand(""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GoogleLoginCommand.IdToken));
    }
}
