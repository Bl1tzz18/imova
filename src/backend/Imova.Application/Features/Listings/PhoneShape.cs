namespace Imova.Application.Features.Listings;

// What the public may see of a listing's phone number before asking for it: its first digits and
// how many follow — enough for the page to draw the number in its real format with the rest as dots
// (+373 22 5•• •••), never the number itself. At least 3 digits stay hidden-proof: at most 5 are
// hidden, and always at least 3 shown.
public static class PhoneShape
{
    public const int HiddenDigits = 5;

    // ("+3732255", 5) for "+373 22 555 010"; null without any digits.
    public static (string Prefix, int HiddenDigits)? For(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = new string(phone.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 0)
        {
            return null;
        }

        var hidden = Math.Max(0, Math.Min(HiddenDigits, digits.Length - 3));
        var plus = phone.TrimStart().StartsWith('+') ? "+" : "";
        return (plus + digits[..^hidden], hidden);
    }
}
