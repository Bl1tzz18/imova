using Imova.Application.Features.Auth.UpdatePhoneNumber;

namespace Imova.UnitTests.Auth;

public class UpdatePhoneNumberValidatorTests
{
    private readonly UpdatePhoneNumberValidator _validator = new();

    [Fact]
    public void Validate_WithValidPhoneNumber_HasNoErrors()
    {
        var result = _validator.Validate(new UpdatePhoneNumberCommand(Guid.NewGuid(), "+373 69 123 456"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyPhoneNumber_HasError()
    {
        var result = _validator.Validate(new UpdatePhoneNumberCommand(Guid.NewGuid(), ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePhoneNumberCommand.PhoneNumber));
    }

    [Theory]
    [InlineData("abc123")]
    [InlineData("123")]
    public void Validate_WithInvalidPhoneNumber_HasError(string phoneNumber)
    {
        var result = _validator.Validate(new UpdatePhoneNumberCommand(Guid.NewGuid(), phoneNumber));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePhoneNumberCommand.PhoneNumber));
    }
}
