namespace Imova.Application.Common.Exceptions;

// A per-user limit was hit (e.g. starting too many conversations in an hour). The global exception
// handler in Program.cs maps this to a 429 with the message as the problem detail.
public sealed class TooManyRequestsException(string message) : Exception(message);
