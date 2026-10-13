namespace Imova.Application.Common;

// The site's languages (the web app's next-intl locales) — what a user's emails are written in
// (ApplicationUser.PreferredLanguage). Romanian is the default: existing accounts, an unknown value,
// an email with nobody behind it.
public static class Languages
{
    public const string Romanian = "ro";
    public const string English = "en";
    public const string Russian = "ru";
    public const string Default = Romanian;

    public static readonly IReadOnlyList<string> Supported = [Romanian, English, Russian];

    public static bool IsSupported(string? value) => value is not null && Supported.Contains(value);

    public static string Normalize(string? value) => IsSupported(value) ? value! : Default;

    // The text for `language`, Romanian for anything else.
    public static string Pick(string language, string ro, string en, string ru) => language switch
    {
        English => en,
        Russian => ru,
        _ => ro,
    };
}
