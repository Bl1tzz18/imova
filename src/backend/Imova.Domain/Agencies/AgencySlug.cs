using System.Globalization;
using System.Text;

namespace Imova.Domain.Agencies;

// An agency's public address: /agencies/{slug}. Made from its name — lower case, Romanian and
// Cyrillic letters spelled in Latin ("Agenția Ușor" → "agentia-usor", "Дом" → "dom"), anything else
// becomes a dash. Uniqueness needs the database: the caller tries FromName, then WithNumber(…, 2),
// WithNumber(…, 3), … until one is free (neither a current slug nor a former one).
public static class AgencySlug
{
    public const int MaxLength = 140;

    // When a name has no letters or digits at all.
    public const string Fallback = "agentie";

    // Words that would read as a page of their own rather than an agency.
    private static readonly HashSet<string> Reserved = ["new", "edit", "admin", "api"];

    // Romanian (both the comma-below ș/ț and the older cedilla ş/ţ) and a few other common accented
    // Latin letters, then Russian plus Moldovan Cyrillic's ӂ. Spelled out rather than via Unicode
    // normalization, which does nothing when .NET runs without ICU (invariant globalization).
    // Any other letter separates words, like punctuation does.
    private static readonly Dictionary<char, string> Letters = new()
    {
        ['ă'] = "a", ['â'] = "a", ['î'] = "i", ['ș'] = "s", ['ş'] = "s", ['ț'] = "t", ['ţ'] = "t",
        ['á'] = "a", ['à'] = "a", ['ä'] = "a", ['é'] = "e", ['è'] = "e", ['ê'] = "e", ['ë'] = "e",
        ['í'] = "i", ['ó'] = "o", ['ö'] = "o", ['ô'] = "o", ['ú'] = "u", ['ü'] = "u", ['ç'] = "c", ['ñ'] = "n",
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['е'] = "e", ['ё'] = "e",
        ['ж'] = "zh", ['ӂ'] = "j", ['з'] = "z", ['и'] = "i", ['й'] = "i", ['к'] = "k", ['л'] = "l",
        ['м'] = "m", ['н'] = "n", ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t",
        ['у'] = "u", ['ф'] = "f", ['х'] = "h", ['ц'] = "ts", ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "sch",
        ['ъ'] = "", ['ы'] = "y", ['ь'] = "", ['э'] = "e", ['ю'] = "iu", ['я'] = "ia",
    };

    // The slug an agency called `name` would like to have; never empty and never reserved — a
    // reserved word comes back numbered ("admin" → "admin-2").
    public static string FromName(string name)
    {
        var slug = Clean(name);
        if (slug.Length == 0)
        {
            return Fallback;
        }

        return Reserved.Contains(slug) ? WithNumber(slug, 2) : slug;
    }

    // Search text spelled the way slugs are ("Agenția Ușor" → "agentia-usor"), so the directory can
    // match a name typed without diacritics, or in Cyrillic, against the slugs. Empty when the text
    // has no letters or digits.
    public static string SearchKey(string text) => Clean(text);

    // "{slug}-{number}", shortening the slug so the result still fits MaxLength.
    public static string WithNumber(string slug, int number)
    {
        if (number < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(number), "Numbering starts at 2.");
        }

        var suffix = "-" + number.ToString(CultureInfo.InvariantCulture);
        var stem = slug.Length + suffix.Length > MaxLength ? slug[..(MaxLength - suffix.Length)].TrimEnd('-') : slug;
        return stem + suffix;
    }

    private static string Clean(string name)
    {
        var lower = name.ToLowerInvariant();
        var builder = new StringBuilder(lower.Length);
        var pendingDash = false;

        foreach (var c in lower)
        {
            string? latin = c is >= 'a' and <= 'z' or >= '0' and <= '9'
                ? c.ToString()
                : Letters.GetValueOrDefault(c);

            if (string.IsNullOrEmpty(latin))
            {
                // Cyrillic's hard/soft signs (empty) vanish; anything else separates words.
                pendingDash |= latin is null;
                continue;
            }

            if (pendingDash && builder.Length > 0)
            {
                builder.Append('-');
            }

            pendingDash = false;
            builder.Append(latin);
        }

        var slug = builder.ToString();
        return slug.Length > MaxLength ? slug[..MaxLength].TrimEnd('-') : slug;
    }
}
