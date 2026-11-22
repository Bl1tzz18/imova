namespace Imova.Application.Common;

// Bound from the "App" configuration section — settings about the site as a whole.
public class AppOptions
{
    public const string SectionName = "App";

    // Where links in emails point (the Next.js site, not the API).
    public string WebBaseUrl { get; set; } = "http://localhost:3000";

    // pathAndQuery starts with "/".
    public string WebUrl(string pathAndQuery) => WebBaseUrl.TrimEnd('/') + pathAndQuery;

    // The same page in a language, as the web app addresses it: Romanian has no prefix (/search),
    // the others do (/ru/search) — see its i18n/localePaths.ts. Links in emails and Telegram use
    // the recipient's language, so they open the site in it.
    public string WebUrl(string pathAndQuery, string? language)
    {
        var normalized = Languages.Normalize(language);
        return WebUrl(normalized == Languages.Default ? pathAndQuery : $"/{normalized}{pathAndQuery}");
    }
}
