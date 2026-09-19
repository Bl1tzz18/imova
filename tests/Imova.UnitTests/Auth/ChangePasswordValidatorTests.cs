using Imova.Application.Features.Auth.ChangePassword;

namespace Imova.UnitTests.Auth;

public class ChangePasswordValidatorTests
{
    private readonly ChangePasswordValidator _validator = new();

    [Fact]
    public void Validate_WithValidNewPassword_HasNoErrors()
    {
        var result = _validator.Validate(new ChangePasswordCommand(Guid.NewGuid(), "OldPassword1", "NewPassword1"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithoutCurrentPassword_HasNoErrors()
    {
        // CurrentPassword is optional at the validator level — ChangePasswordHandler decides
        // whether it's actually required (only for accounts that already have a password).
        var result = _validator.Validate(new ChangePasswordCommand(Guid.NewGuid(), null, "NewPassword1"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyNewPassword_HasError()
    {
        var result = _validator.Validate(new ChangePasswordCommand(Guid.NewGuid(), "OldPassword1", ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_WithNewPasswordShorterThanEightChars_HasError()
    {
        var result = _validator.Validate(new ChangePasswordCommand(Guid.NewGuid(), "OldPassword1", "Ab1defg"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_WithNewPasswordTooLong_HasError()
    {
        var result = _validator.Validate(new ChangePasswordCommand(Guid.NewGuid(), "OldPassword1", new string('a', 101)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }
}
