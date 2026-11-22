using System.Net;
using Imova.Application.Common.Interfaces;

namespace Imova.Application.Features.Telegram.Bot;

// Telegram's HTML formatting (parse_mode=HTML): only &, < and > (and " inside attributes) need
// escaping. Telegram refuses links to addresses it can't reach — localhost, private networks — in
// buttons ("BUTTON_URL_INVALID", the whole message is rejected), so with a local App:WebBaseUrl
// links become plain text instead.
public static class TelegramHtml
{
    public static string Encode(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    public static string Link(string text, string url) =>
        CanLink(url) ? $"<a href=\"{Encode(url).Replace("\"", "&quot;")}\">{Encode(text)}</a>" : $"{Encode(text)} ({Encode(url)})";

    // The message with its link buttons — or, for addresses Telegram won't take, the links written
    // out at the end of the text.
    public static (string Html, IReadOnlyList<TelegramButton> Buttons) WithButtons(string html, params TelegramButton[] buttons)
    {
        var usable = buttons.Where(b => CanLink(b.Url)).ToList();
        var written = buttons.Where(b => !CanLink(b.Url)).Select(b => $"{Encode(b.Text)}: {Encode(b.Url)}").ToList();
        return (written.Count == 0 ? html : html + "\n\n" + string.Join("\n", written), usable);
    }

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
