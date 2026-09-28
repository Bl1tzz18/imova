using FluentValidation.Results;
using Imova.Application.Common;
using Imova.Application.Common.Validation;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth;

// Turns ASP.NET Identity's errors into validation failures on the right field, coded
// "identity.<Code>" for the web app to translate (see ErrorCodes).
public static class IdentityFailures
{
    public static IEnumerable<ValidationFailure> From(
        IEnumerable<IdentityError> errors, string emailField, string passwordField, string? currentPasswordField = null)
    {
        var list = errors.ToList();

        // The username *is* the email (see RegisterHandler), so a taken email always comes back
        // twice — once as DuplicateUserName, once as DuplicateEmail. Say it once.
        if (list.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateEmail)))
        {
            list.RemoveAll(e => e.Code == nameof(IdentityErrorDescriber.DuplicateUserName));
        }

        return list.Select(e => CodedFailure.Of(FieldFor(e.Code), e.Description, ErrorCodes.IdentityPrefix + e.Code));

        string FieldFor(string code) => code switch
        {
            // ChangePasswordAsync's "Incorrect password." is about the *current* password.
            nameof(IdentityErrorDescriber.PasswordMismatch) when currentPasswordField is not null => currentPasswordField,
            _ when code.StartsWith("Password", StringComparison.Ordinal) => passwordField,
            _ => emailField,
        };
    }
}
