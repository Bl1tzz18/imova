using Imova.Application.Features.Auth;
using Microsoft.AspNetCore.Identity;

namespace Imova.UnitTests.Auth;

public class IdentityFailuresTests
{
    private static readonly IdentityErrorDescriber Describer = new();

    [Fact]
    public void ATakenEmail_IsReportedOnce_NotAlsoAsATakenUsername()
    {
        // The username is the email, so Identity reports both.
        var failures = IdentityFailures.From(
            [Describer.DuplicateUserName("ana@example.com"), Describer.DuplicateEmail("ana@example.com")],
            emailField: "Email",
            passwordField: "Password").ToList();

        var failure = Assert.Single(failures);
        Assert.Equal("identity.DuplicateEmail", failure.ErrorCode);
        Assert.Equal("Email", failure.PropertyName);
    }

    [Fact]
    public void PasswordErrors_GoOnThePasswordField_AWrongCurrentPasswordOnTheCurrentPasswordField()
    {
        var failures = IdentityFailures.From(
            [Describer.PasswordTooShort(8), Describer.PasswordMismatch()],
            emailField: "NewPassword",
            passwordField: "NewPassword",
            currentPasswordField: "CurrentPassword").ToList();

        Assert.Contains(failures, f => f.ErrorCode == "identity.PasswordTooShort" && f.PropertyName == "NewPassword");
        Assert.Contains(failures, f => f.ErrorCode == "identity.PasswordMismatch" && f.PropertyName == "CurrentPassword");
    }
}
