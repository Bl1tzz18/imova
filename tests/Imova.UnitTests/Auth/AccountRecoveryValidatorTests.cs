using Imova.Application.Features.Auth.ConfirmEmail;
using Imova.Application.Features.Auth.ForgotPassword;
using Imova.Application.Features.Auth.ResetPassword;

namespace Imova.UnitTests.Auth;

// ForgotPassword, ResetPassword and ConfirmEmail validators — small enough to share a file.
public class AccountRecoveryValidatorTests
{
    [Fact]
    public void ForgotPassword_WithAValidEmail_HasNoErrors()
    {
        Assert.True(new ForgotPasswordValidator().Validate(new ForgotPasswordCommand("ana@example.com")).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void ForgotPassword_WithABadEmail_HasAnError(string email)
    {
        var result = new ForgotPasswordValidator().Validate(new ForgotPasswordCommand(email));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ForgotPasswordCommand.Email));
    }

    [Fact]
    public void ResetPassword_WithAllFields_HasNoErrors()
    {
        var result = new ResetPasswordValidator().Validate(new ResetPasswordCommand("ana@example.com", "dG9rZW4", "NewPassword1!"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ResetPassword_WithAShortPassword_HasAnError()
    {
        var result = new ResetPasswordValidator().Validate(new ResetPasswordCommand("ana@example.com", "dG9rZW4", "short"));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ResetPasswordCommand.NewPassword));
    }

    [Fact]
    public void ResetPassword_WithoutAToken_HasAnError()
    {
        var result = new ResetPasswordValidator().Validate(new ResetPasswordCommand("ana@example.com", "", "NewPassword1!"));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ResetPasswordCommand.Token));
    }

    [Fact]
    public void ConfirmEmail_WithoutAUserIdOrToken_HasBothErrors()
    {
        var result = new ConfirmEmailValidator().Validate(new ConfirmEmailCommand(Guid.Empty, ""));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmEmailCommand.UserId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmEmailCommand.Token));
    }
}
