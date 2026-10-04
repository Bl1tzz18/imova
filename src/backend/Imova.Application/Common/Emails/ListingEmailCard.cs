namespace Imova.Application.Common.Emails;

// A listing as a card in an email, like on the site: its main photo, the price line, title,
// kind/size and place — the whole card links to the listing. CompactHtml() is the compact row (photo on
// the left, for lists of listings); LargeHtml() puts a full-width photo on top, for an email about
// one listing — stacked, so it reads well on a phone without media queries (many mail apps ignore
// them). PriceHtml is already HTML (the caller encodes what's in it: a struck old price, a badge…);
// every other field is plain text and encoded here. Tables and inline styles only.
public record ListingEmailCard(
    string Title,
    string Url,
    string? PhotoUrl,
    string PriceHtml,
    string Facts,
    string? Location,
    // A small label over the text, e.g. "Vândut".
    string? Badge = null)
{
    private static string E(string value) => EmailLayout.Encode(value);

    private string TextHtml()
    {
        var badge = Badge is { } text
            ? $"""<div style="margin-bottom:6px;"><span style="display:inline-block;padding:2px 10px;border-radius:999px;background:#fdebe2;color:#b4481a;font-size:12px;font-weight:700;">{E(text)}</span></div>"""
            : string.Empty;
        var location = Location is { } place
            ? $"""<div style="margin-top:2px;font-size:13px;color:#4d6e8c;">{E(place)}</div>"""
            : string.Empty;

        return $"""
            {badge}
            <div style="font-size:18px;font-weight:700;color:#0b1620;">{PriceHtml}</div>
            <div style="margin-top:4px;font-size:15px;line-height:1.35;color:#101f2e;">{E(Title)}</div>
            <div style="margin-top:6px;font-size:13px;color:#4d6e8c;">{E(Facts)}</div>
            {location}
            """;
    }

    public string LargeHtml()
    {
        // 544px = the 600px email minus its side padding; the 800px card size fills it on a phone too.
        var photo = PhotoUrl is { } url
            ? $"""<tr><td style="padding:0;"><img src="{E(url)}" width="544" alt="{E(Title)}" style="display:block;width:100%;max-width:544px;height:auto;border:0;border-radius:12px 12px 0 0;"></td></tr>"""
            : string.Empty;

        return $"""
            <tr><td style="padding:8px 28px;">
              <a href="{E(Url)}" style="text-decoration:none;color:#0b1620;display:block;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border:1px solid #e5edf3;border-radius:12px;">
                  {photo}
                  <tr><td style="padding:14px 16px 16px;">{TextHtml()}</td></tr>
                </table>
              </a>
            </td></tr>
            """;
    }

    public string CompactHtml()
    {
        var photo = PhotoUrl is { } url
            ? $"""<img src="{E(url)}" width="144" height="108" alt="{E(Title)}" style="display:block;width:144px;height:108px;object-fit:cover;border:0;border-radius:8px;">"""
            : """<div style="width:144px;height:108px;line-height:108px;border-radius:8px;background:#e2f1f6;color:#57a8c4;font-size:12px;text-align:center;">fără foto</div>""";
        return $"""
            <tr><td style="padding:6px 28px;">
              <a href="{E(Url)}" style="text-decoration:none;color:#0b1620;display:block;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border:1px solid #e5edf3;border-radius:12px;">
                  <tr>
                    <td width="168" valign="top" style="padding:12px;">{photo}</td>
                    <td valign="top" style="padding:12px 14px 12px 0;">{TextHtml()}</td>
                  </tr>
                </table>
              </a>
            </td></tr>
            """;
    }
}
