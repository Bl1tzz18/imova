using FluentValidation.Results;

namespace Imova.Api.Common;

// The language-neutral part of every error response (see Imova.Application.Common.ErrorCodes),
// added to the ProblemDetails as extensions — the web app translates these, not `detail`/`errors`:
//   a problem:            "code": "auth.lockedOut", "params": { ... }
//   a validation problem: "errorCodes": [{ "field": "Password", "code": "password.noNumber", "params": { ... } }]
public static class ProblemCodes
{
    // FluentValidation placeholders worth passing on for built-in rules (MaximumLength etc.),
    // under the short names the web app's messages use.
    private static readonly Dictionary<string, string> PlaceholderParams = new()
    {
        ["MinLength"] = "min",
        ["MaxLength"] = "max",
        ["From"] = "from",
        ["To"] = "to",
        ["ComparisonValue"] = "value",
    };

    public static Dictionary<string, object?> For(string code, IReadOnlyDictionary<string, object>? parameters = null)
    {
        var extensions = new Dictionary<string, object?> { ["code"] = code };
        if (parameters is { Count: > 0 })
        {
            extensions["params"] = parameters;
        }

        return extensions;
    }

    public static Dictionary<string, object?> ForValidation(IEnumerable<ValidationFailure> failures) => new()
    {
        ["errorCodes"] = failures
            .Select(f => new { field = f.PropertyName, code = f.ErrorCode, @params = ParamsOf(f) })
            .ToList(),
    };

    private static IReadOnlyDictionary<string, object>? ParamsOf(ValidationFailure failure)
    {
        if (failure.CustomState is IReadOnlyDictionary<string, object> custom)
        {
            return custom;
        }

        var fromPlaceholders = (failure.FormattedMessagePlaceholderValues ?? [])
            .Where(kv => PlaceholderParams.ContainsKey(kv.Key) && kv.Value is not null)
            .ToDictionary(kv => PlaceholderParams[kv.Key], kv => kv.Value);
        return fromPlaceholders.Count > 0 ? fromPlaceholders : null;
    }
}
