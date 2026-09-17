namespace Imova.Application.Common.Exceptions;

// Thrown for invalid credentials (wrong email/password) or a Google ID token that fails
// verification — as opposed to a validation error (missing/malformed input) or a permissions
// error on an otherwise-valid, authenticated request. The global exception handler in Program.cs
// maps this to a 401.
public sealed class AuthenticationFailedException(string message) : Exception(message);
