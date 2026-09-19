using Imova.Application.Features.Auth.UpdateProfile;

namespace Imova.UnitTests.Auth;

public class UpdateProfileValidatorTests
{
    private readonly UpdateProfileValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new UpdateProfileCommand(Guid.NewGuid(), "Display Name", "+373 69 123 456"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithNullDisplayName_HasNoErrors()
    {
        var result = _validator.Validate(new UpdateProfileCommand(Guid.NewGuid(), null, "+373 69 123 456"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithDisplayNameTooLong_HasError()
    {
        var result = _validator.Validate(new UpdateProfileCommand(Guid.NewGuid(), new string('a', 201), "+373 69 123 456"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileCommand.DisplayName));
    }

    [Fact]
    public void Validate_WithEmptyPhoneNumber_HasError()
    {
        var result = _validator.Validate(new UpdateProfileCommand(Guid.NewGuid(), "Display Name", ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileCommand.PhoneNumber));
    }
}
