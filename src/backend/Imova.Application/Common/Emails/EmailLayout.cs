using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Imova.Application.Common.Emails;

// The look every HTML email shares: a white card on the site's grey background with the IMOVA
// header, content rows, the orange button and a small footer. Email HTML is its own world —
// tables and inline styles only, no external CSS, no scripts — so the pieces are plain strings.
// Every helper that takes text encodes it; the ones that take `html` expect the caller to have
// encoded any user-written values with Encode.
public static class EmailLayout
{
    // Escapes what matters in HTML (<, >, &, quotes) but leaves Romanian/Russian letters readable
    // (WebUtility.HtmlEncode would turn "â" into "&#226;").
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    public static string Encode(string value) => Encoder.Encode(value);

    public static string Heading(string text) =>
        $"""<tr><td style="padding:8px 28px 2px;font-size:20px;font-weight:700;color:#0b1620;">{Encode(text)}</td></tr>""";

    public static string Paragraph(string html) =>
        $"""<tr><td style="padding:8px 28px;font-size:15px;line-height:1.55;color:#22394f;">{html}</td></tr>""";

    public static string Button(string text, string url) =>
        $"""
        <tr><td align="center" style="padding:20px 28px 10px;">
          <a href="{Encode(url)}" style="display:inline-block;padding:12px 28px;border-radius:999px;background:#e86a33;color:#ffffff;font-size:15px;font-weight:700;text-decoration:none;">{Encode(text)}</a>
        </td></tr>
        """;

    // For buttons that carry a one-time link: some mail apps block buttons, so show the address too.
    public static string LinkFallback(string url) =>
        $"""
        <tr><td style="padding:6px 28px 4px;font-size:12px;line-height:1.5;color:#7592ac;">
          Dacă butonul nu funcționează, copiază acest link în browser:<br>
          <a href="{Encode(url)}" style="color:#4d6e8c;word-break:break-all;">{Encode(url)}</a>
        </td></tr>
        """;

    public static string Page(string title, string content, string footerHtml) =>
        $"""
        <!DOCTYPE html>
        <html lang="ro">
        <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{Encode(title)}</title></head>
        <body style="margin:0;padding:0;background:#eef2f6;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#eef2f6;">
            <tr><td align="center" style="padding:24px 12px;">
              <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="width:100%;max-width:600px;background:#ffffff;border-radius:16px;font-family:Arial,Helvetica,sans-serif;color:#0b1620;">
                <tr><td style="padding:24px 28px 4px;font-size:22px;font-weight:800;letter-spacing:0.5px;color:#e86a33;">IMOVA</td></tr>
                {content}
                <tr><td style="padding:16px 28px 24px;border-top:1px solid #e5edf3;font-size:12px;line-height:1.5;color:#7592ac;">{footerHtml}</td></tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
}
