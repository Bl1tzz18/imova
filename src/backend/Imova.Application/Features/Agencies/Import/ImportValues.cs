using System.Globalization;

namespace Imova.Application.Features.Agencies.Import;

// Reading one cell of an import row. Lenient where a spreadsheet or a script commonly differs, strict
// where guessing could change the meaning:
// - numbers: "78000", "78 000" (spaces as thousands), "58.5" or "58,5"; not "1,234.5" (ambiguous);
// - yes/no: da/nu, yes/no, true/false, 1/0;
// - choices: case, spaces, "_" and "-" don't matter ("own_boiler" = "OwnBoiler");
// - dates: 2026-11-01 or 01.11.2026;
// - lists: "a|b|c", empty items dropped.
public static class ImportValues
{
    private static readonly string[] Yes = ["da", "yes", "true", "1"];
    private static readonly string[] No = ["nu", "no", "false", "0"];
    private static readonly string[] DateFormats = ["yyyy-MM-dd", "dd.MM.yyyy", "d.M.yyyy"];

    public static bool TryDecimal(string value, out decimal result)
    {
        result = 0;
        var text = WithoutSpaces(value);
        if (text.Contains('.') && text.Contains(','))
        {
            return false;
        }

        return decimal.TryParse(
            text.Replace(',', '.'), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);
    }

    public static bool TryInt(string value, out int result) =>
        int.TryParse(WithoutSpaces(value), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);

    public static bool TryYesNo(string value, out bool result)
    {
        var text = value.Trim().ToLowerInvariant();
        result = Yes.Contains(text);
        return result || No.Contains(text);
    }

    public static bool TryDate(string value, out DateOnly result) =>
        DateOnly.TryParseExact(value.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);

    // One of `choices` (the canonical spellings), matched loosely; null when nothing matches.
    public static string? MatchChoice(string value, IEnumerable<string> choices)
    {
        var key = ChoiceKey(value);
        return key.Length == 0 ? null : choices.FirstOrDefault(choice => ChoiceKey(choice) == key);
    }

    public static bool TryEnum<TEnum>(string value, out TEnum result)
        where TEnum : struct, Enum
    {
        result = default;
        var name = MatchChoice(value, Enum.GetNames<TEnum>());
        return name is not null && Enum.TryParse(name, out result);
    }

    public static IReadOnlyList<string> List(string value) =>
        value.Split(ImportColumns.ListSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static string ChoiceKey(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static string WithoutSpaces(string value) =>
        new(value.Where(c => !char.IsWhiteSpace(c) && c != ' ' && c != ' ').ToArray());
}
