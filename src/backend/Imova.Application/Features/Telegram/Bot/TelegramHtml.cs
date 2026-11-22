using System.Net;
using Imova.Application.Common.Interfaces;

namespace Imova.Application.Features.Telegram.Bot;

// Telegram's HTML formatting (parse_mode=HTML): only &, < and > (and " inside attributes) need
// escaping. Telegram won't link to addresses it can't reach — localhost, private networks: a
// button to one rejects the whole message ("BUTTON_URL_INVALID"), a link in the text is silently
// dropped. So with a local App:WebBaseUrl the links are simply left out (on the real domain they're
// buttons and links as usual).
public static class TelegramHtml
{
    public static string Encode(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    public static string Link(string text, string url) =>
        CanLink(url) ? $"<a href=\"{Encode(url).Replace("\"", "&quot;")}\">{Encode(text)}</a>" : Encode(text);

    // The message with the buttons Telegram will take: link buttons to an address it can't reach are
    // left out; callback buttons always stay.
    public static (string Html, IReadOnlyList<TelegramButton> Buttons) WithButtons(string html, params TelegramButton[] buttons) =>
        (html, buttons.Where(b => b.Url is null || CanLink(b.Url)).ToList());

    public static bool CanLink(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return false;
        }

        if (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host == "0.0.0.0")
        {
            return false;
        }

        return !IPAddress.TryParse(uri.Host, out var ip) || !IsPrivate(ip);
    }

    private static bool IsPrivate(IPAddress ip)
    {
        if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal)
        {
            return true;
        }

        var bytes = ip.MapToIPv4().GetAddressBytes();
        return bytes[0] == 10 || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) || (bytes[0] == 192 && bytes[1] == 168)
               || (bytes[0] == 169 && bytes[1] == 254);
    }
}
