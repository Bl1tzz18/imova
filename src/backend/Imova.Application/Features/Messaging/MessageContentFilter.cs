using System.Text.RegularExpressions;

namespace Imova.Application.Features.Messaging;

// v1 scam heuristics: a message matching any of these is still delivered, but flagged for admin
// review (see GetFlaggedMessages). Deliberately a plain pattern list — easy to read and extend.
// Matching is case-insensitive and ignores Romanian diacritics (see Fold), so patterns are
// written without them.
//
// The patterns run with NonBacktracking: linear in the message's length whatever it contains, about
// 0.2 ms at most per rule on a 2,000-character message. Building them takes ~0.5 s once per process,
// and each one's first match builds more of it — so they live in their own class (Fold doesn't load
// them) and are warmed up once (WarmUp, at API start-up), never inside a send's 200 ms timeout.
public static class MessageContentFilter
{
    // A message a rule couldn't finish checking in time (e.g. the server was starved for CPU) is
    // still delivered, flagged with this reason so admins look at it — never a failed send.
    public const string TimeoutReason = "Filter timeout";

    // The first matching rule's reason, or null for a clean message.
    public static string? Check(string? body) => Check(body, DefaultRules.Rules);

    // Builds and exercises every rule once, so no send pays for that.
    public static void WarmUp() => Check("warm-up: plata in avans, cod din sms, https://exemplu.ru, золотая корона 123");

    // The same, with the given rules (tests use it to force a timeout).
    public static string? Check(string? body, IReadOnlyList<(string Reason, Regex Pattern)> rules)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        var folded = Fold(body);
        foreach (var (reason, pattern) in rules)
        {
            try
            {
                if (pattern.IsMatch(folded))
                {
                    return reason;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return TimeoutReason;
            }
        }

        return null;
    }

    // Lowercase, without Romanian diacritics ("Plătește" -> "plateste"). An explicit map rather than
    // Unicode normalization, which isn't available when the app runs with invariant globalization.
    public static string Fold(string value)
    {
        var chars = value.ToLowerInvariant().ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = chars[i] switch
            {
                'ă' or 'â' => 'a',
                'î' => 'i',
                'ș' or 'ş' => 's',
                'ț' or 'ţ' => 't',
                _ => chars[i],
            };
        }

        return new string(chars);
    }

    private static class DefaultRules
    {
        public static readonly (string Reason, Regex Pattern)[] Rules =
        [
            ("Money transfer service", Pattern(@"\b(western\s*union|moneygram|ria\s+money|zolotaya\s*korona|золотая\s*корона)\b")),
            ("Advance payment request", Pattern(@"\b(plata|plateste|achita|transfera|trimite)\w*\s+(in\s+)?avans\b|\bavans\w*\s+(pentru|ca\s+sa)\b|\b(advance|upfront)\s+payment\b|\bpay\s+(a\s+)?deposit\s+(first|before)\b|\bпредоплат\w*")),
            ("Card or verification details", Pattern(@"\b(numarul|datele|codul)\s+(cardului|de\s+pe\s+card)\b|\bcod(ul)?\s+(din\s+sms|de\s+verificare)\b|\bcvv\b|\bcard\s+number\b|\bverification\s+code\b|\bкод\s+из\s+смс\b|\bномер\s+карты\b")),
            ("Off-platform payment", Pattern(@"\b(paypal|revolut|bitcoin|btc|usdt|crypto\w*|криптовалют\w*)\b|\bgift\s*card\b")),
            ("External link", Pattern(@"https?://|www\.|\b\w+\.(ru|xyz|top|click|link)\b")),
        ];

        private static Regex Pattern(string pattern) =>
            new(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(200));
    }
}
