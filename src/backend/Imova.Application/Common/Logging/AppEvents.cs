using System.Collections;
using System.Reflection;

namespace Imova.Application.Common.Logging;

// Business-event log lines. Every one carries an "Event" property (the command's name without
// "Command", or a name like "RateLimited"), so in Application Insights they are
// `traces | where isnotempty(customDimensions.Event)`.
//
// Only ids go into an event: the Guid-valued properties of the command and of its result (one level
// deep, e.g. AuthResultDto.User.Id → "UserId"). Never emails, names, phones or text — by
// construction, so a new property can't leak into the logs by accident. Logs are kept at most 90
// days (the privacy policy's retention table).
public static class AppEvents
{
    public const string Category = "Imova.Events";

    public static string NameOf(Type commandType) =>
        commandType.Name.EndsWith("Command", StringComparison.Ordinal) ? commandType.Name[..^"Command".Length] : commandType.Name;

    public static IReadOnlyDictionary<string, object> Ids(object? command, object? result = null)
    {
        var ids = new SortedDictionary<string, object>(StringComparer.Ordinal);
        AddIds(ids, command, prefix: "", depth: 0);
        AddIds(ids, result, prefix: "", depth: 0);
        return ids;
    }

    // "ListingId=…, UserId=…" — the same ids in the message text, for reading logs in a console.
    public static string Describe(IReadOnlyDictionary<string, object> ids) =>
        string.Join(", ", ids.Select(pair => $"{pair.Key}={pair.Value}"));

    private static void AddIds(IDictionary<string, object> ids, object? source, string prefix, int depth)
    {
        if (source is null || source is string || source is IEnumerable)
        {
            return;
        }

        foreach (var property in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (type == typeof(Guid))
            {
                if (property.GetValue(source) is Guid value && value != Guid.Empty)
                {
                    // Nested: "User" + "Id" → "UserId", "Publisher" + "Id" → "PublisherId".
                    ids.TryAdd(prefix + property.Name, value);
                }
            }
            else if (depth == 0 && type.IsClass && type != typeof(string) && !typeof(IEnumerable).IsAssignableFrom(type))
            {
                AddIds(ids, property.GetValue(source), property.Name, depth + 1);
            }
        }
    }
}
