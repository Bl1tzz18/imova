using FluentValidation.Results;

namespace Imova.Application.Common.Validation;

// A validation failure carrying an ErrorCodes code (and any numbers the translated message needs,
// e.g. { "max": 5 }) — for failures raised by hand in handlers. Rule chains do the same with
// .WithErrorCode(code).WithState(_ => CodedFailure.Params(...)). The global exception handler sends
// both to the web app as `errorCodes` (see ErrorCodes).
public static class CodedFailure
{
    public static ValidationFailure Of(string field, string message, string code, IReadOnlyDictionary<string, object>? parameters = null) =>
        new(field, message) { ErrorCode = code, CustomState = parameters };

    public static IReadOnlyDictionary<string, object> Params(params (string Key, object Value)[] items) =>
        items.ToDictionary(i => i.Key, i => i.Value);
}
