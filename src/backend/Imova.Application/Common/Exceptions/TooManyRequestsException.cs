namespace Imova.Application.Common.Exceptions;

// A per-user limit was hit (e.g. starting too many conversations in an hour). The global exception
// handler in Program.cs maps this to a 429 with the message as the problem detail, plus Code (an
// ErrorCodes value) and Params (numbers the translated message needs, e.g. { "max": 10 }).
public sealed class TooManyRequestsException(
    string message,
    string code = ErrorCodes.TooManyRequests,
    IReadOnlyDictionary<string, object>? parameters = null)
    : Exception(message)
{
    public string Code { get; } = code;

    public IReadOnlyDictionary<string, object>? Params { get; } = parameters;
}
