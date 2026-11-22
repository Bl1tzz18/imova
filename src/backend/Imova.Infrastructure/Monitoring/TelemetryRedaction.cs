using System.Diagnostics;
using System.Text.RegularExpressions;
using OpenTelemetry;

namespace Imova.Infrastructure.Monitoring;

// What leaves for Application Insights must not carry secrets or more personal data than needed:
// - the Telegram Bot API puts the bot's token in the path (https://api.telegram.org/bot<token>/getUpdates);
// - query strings can hold tokens (blob SAS signatures, links) and searched addresses (Nominatim) —
//   only the parameter names are kept, never the values.
// Applied to every finished span's URL tags (TelemetryRedactionProcessor), API and Worker alike.
public static partial class TelemetryRedaction
{
    public const string Redacted = "REDACTED";

    // OpenTelemetry's current names and the older ones some instrumentations still write.
    public static readonly string[] UrlTags = ["url.full", "url.query", "url.path", "http.url", "http.target"];

    public static string Url(string value)
    {
        var redacted = BotToken().Replace(value, "/bot" + Redacted);
        var queryStart = redacted.IndexOf('?');
        if (queryStart < 0)
        {
            return redacted;
        }

        var fragmentStart = redacted.IndexOf('#', queryStart);
        var query = fragmentStart < 0 ? redacted[(queryStart + 1)..] : redacted[(queryStart + 1)..fragmentStart];
        var names = query.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2)[0] + "=" + Redacted);
        var fragment = fragmentStart < 0 ? "" : redacted[fragmentStart..];
        return redacted[..queryStart] + "?" + string.Join('&', names) + fragment;
    }

    // url.query holds just the query ("a=1&b=2", sometimes with its "?"), the other tags a path or a full URL.
    public static string Tag(string tag, string value) =>
        tag == "url.query" && !value.StartsWith('?') ? Url("?" + value)[1..] : Url(value);

    [GeneratedRegex(@"/bot[^/?#]+", RegexOptions.CultureInvariant)]
    private static partial Regex BotToken();
}

public sealed class TelemetryRedactionProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        foreach (var tag in TelemetryRedaction.UrlTags)
        {
            if (activity.GetTagItem(tag) is string value)
            {
                activity.SetTag(tag, TelemetryRedaction.Tag(tag, value));
            }
        }

        activity.DisplayName = TelemetryRedaction.Url(activity.DisplayName);
    }
}
