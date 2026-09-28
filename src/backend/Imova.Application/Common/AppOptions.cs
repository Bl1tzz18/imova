namespace Imova.Application.Common;

// Bound from the "App" configuration section — settings about the site as a whole.
public class AppOptions
{
    public const string SectionName = "App";

    // Where links in emails point (the Next.js site, not the API).
    public string WebBaseUrl { get; set; } = "http://localhost:3000";

    // pathAndQuery starts with "/".
    public string WebUrl(string pathAndQuery) => WebBaseUrl.TrimEnd('/') + pathAndQuery;
}
