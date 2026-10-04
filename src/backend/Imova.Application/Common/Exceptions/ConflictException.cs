namespace Imova.Application.Common.Exceptions;

// The request clashes with how things already are (e.g. inviting someone who is already a member).
// The global exception handler in Program.cs maps this to a 409, with Code (an ErrorCodes value).
public sealed class ConflictException(string message, string code) : Exception(message)
{
    public string Code { get; } = code;
}
