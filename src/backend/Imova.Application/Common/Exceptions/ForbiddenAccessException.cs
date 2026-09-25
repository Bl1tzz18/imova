namespace Imova.Application.Common.Exceptions;

// Thrown when an authenticated caller isn't allowed to act on a specific resource (e.g. editing
// or deleting someone else's listing) — as opposed to not being authenticated at all. The global
// exception handler in Program.cs maps this to a 403.
public sealed class ForbiddenAccessException(string message = "You do not have permission to perform this action.")
    : Exception(message);
