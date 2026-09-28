namespace Imova.Application.Common.Exceptions;

// Thrown for invalid credentials (wrong email/password) or a Google ID token that fails
// verification — as opposed to a validation error (missing/malformed input) or a permissions
// error on an otherwise-valid, authenticated request. The global exception handler in Program.cs
// maps this to a 401, with Code (an ErrorCodes value) for the web app to translate.
public sealed class AuthenticationFailedException(string message, string code = ErrorCodes.AuthFailed) : Exception(message)
{
    public string Code { get; } = code;
}
