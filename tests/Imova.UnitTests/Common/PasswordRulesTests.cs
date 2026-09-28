using Imova.Application.Features.Auth.ChangePassword;
using Imova.Application.Features.Auth.Register;
using Imova.Application.Features.Auth.ResetPassword;

namespace Imova.UnitTests.Common;

// The password policy (PasswordRules): 8+ characters, a number, a special character — checked
// through each validator that uses it, so none of them can drift from the others.
public class PasswordRulesTests
{
    public static TheoryData<string, string> Rejected => new()
    {
        { "Ab1!", "at least 8" },
        { "NoNumbers!!", "number" },
        { "NoSpecial123", "special" },
        { "", "must not be empty" },
    };

    [Theory]
    [InlineData("parola12!")]
    [InlineData("12345678 ")]  // a space counts as a special character, as in Identity
    [InlineData("Ștefănescu#1")]
    public void AcceptedPasswords_PassEveryValidator(string password)
    {
        Assert.True(new RegisterValidator().Validate(new RegisterCommand("a@example.com", password, null, "+373 69 123 456")).IsValid);
        Assert.True(new ChangePasswordValidator().Validate(new ChangePasswordCommand(Guid.NewGuid(), null, password)).IsValid);
        Assert.True(new ResetPasswordValidator().Validate(new ResetPasswordCommand("a@example.com", "dG9rZW4", password)).IsValid);
    }

    [Theory]
    [MemberData(nameof(Rejected))]
    public void RejectedPasswords_FailEveryValidator_WithAMessageSayingWhy(string password, string expectedMessagePart)
    {
        var register = new RegisterValidator().Validate(new RegisterCommand("a@example.com", password, null, "+373 69 123 456"));
        var change = new ChangePasswordValidator().Validate(new ChangePasswordCommand(Guid.NewGuid(), null, password));
        var reset = new ResetPasswordValidator().Validate(new ResetPasswordCommand("a@example.com", "dG9rZW4", password));

        Assert.Contains(register.Errors, e => e.PropertyName == nameof(RegisterCommand.Password) && e.ErrorMessage.Contains(expectedMessagePart));
        Assert.Contains(change.Errors, e => e.PropertyName == nameof(ChangePasswordCommand.NewPassword) && e.ErrorMessage.Contains(expectedMessagePart));
        Assert.Contains(reset.Errors, e => e.PropertyName == nameof(ResetPasswordCommand.NewPassword) && e.ErrorMessage.Contains(expectedMessagePart));
    }
}
