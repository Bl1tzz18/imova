using System.Globalization;
using System.Text;
using Imova.Application.Common.Emails;
using Imova.Application.Common.Interfaces;

namespace Imova.Application.Features.SavedSearches.Alerts;

// One listing as it appears in an alert email.
public record AlertListing(
    Guid Id,
    string Title,
    decimal Amount,
    string Currency,
    string TransactionType,
    string PropertyType,
    decimal AreaM2,
    string? Location,
    string? PhotoUrl,
    string Url);

// The saved-search alert email: an HTML version (a card per listing with its main photo, price,
// type, area and location; a button to the results; the unsubscribe link) and the same content as
// plain text for clients that don't show HTML. Romanian, like the other emails for now. Email HTML
// is its own world: tables and inline styles only, fixed-size images, no external CSS or scripts.
// Every user-written value (titles, the search name) is HTML-encoded.
public static class SavedSearchAlertEmail
{
    private static readonly Dictionary<string, string> PropertyTypes = new()
    {
        ["Apartment"] = "Apartament",
        ["House"] = "Casă",
        ["Land"] = "Teren",
        ["Commercial"] = "Spațiu comercial",
        ["Garage"] = "Garaj",
        ["Room"] = "Cameră",
    };

    public static EmailMessage Build(
        string to, string searchName, IReadOnlyList<AlertListing> listings, int total, string openUrl, string unsubscribeUrl)
    {
        var headline = total == 1 ? "Un anunț nou" : $"{total} anunțuri noi";
        var subject = $"{headline} pentru „{searchName}” — IMOVA";
        var more = total - listings.Count;

        return new EmailMessage(to, subject, Text(searchName, listings, more, openUrl, unsubscribeUrl), Html(headline, searchName, listings, more, openUrl, unsubscribeUrl));
    }

    // "9 000 EUR" / "450 EUR / lună"
    public static string Price(AlertListing listing) =>
        $"{listing.Amount.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ')} {listing.Currency}"
        + (listing.TransactionType == "Rent" ? " / lună" : string.Empty);

    // "Garaj · Vânzare · 18 m²"
    public static string Facts(AlertListing listing) => string.Join(
        " · ",
        PropertyTypes.GetValueOrDefault(listing.PropertyType, listing.PropertyType),
        listing.TransactionType == "Rent" ? "Chirie" : "Vânzare",
        $"{listing.AreaM2.ToString("0.##", CultureInfo.InvariantCulture)} m²");

    private static string Text(string searchName, IReadOnlyList<AlertListing> listings, int more, string openUrl, string unsubscribeUrl)
    {
        var text = new StringBuilder();
        text.Append($"Au apărut anunțuri noi pentru căutarea ta salvată „{searchName}”:\n\n");
        foreach (var listing in listings)
        {
            text.Append($"• {listing.Title} — {Price(listing)}\n  {Facts(listing)}");
            if (listing.Location is { } location) text.Append($" · {location}");
            text.Append($"\n  {listing.Url}\n\n");
        }

        if (more > 0) text.Append($"…și încă {more}.\n\n");
        text.Append($"Vezi toate rezultatele: {openUrl}\n\n");
        text.Append($"Nu mai vrei aceste emailuri? Oprește alertele pentru această căutare:\n{unsubscribeUrl}\n");
        return text.ToString();
    }

    private static string Html(string headline, string searchName, IReadOnlyList<AlertListing> listings, int more, string openUrl, string unsubscribeUrl)
    {
        static string E(string value) => EmailLayout.Encode(value);

        var cards = new StringBuilder();
        foreach (var listing in listings)
        {
            var photo = listing.PhotoUrl is { } url
                ? $"""<img src="{E(url)}" width="144" height="108" alt="{E(listing.Title)}" style="display:block;width:144px;height:108px;object-fit:cover;border:0;border-radius:8px;">"""
                : """<div style="width:144px;height:108px;line-height:108px;border-radius:8px;background:#e2f1f6;color:#57a8c4;font-size:12px;text-align:center;">fără foto</div>""";
            var location = listing.Location is { } place
                ? $"""<div style="margin-top:2px;font-size:13px;color:#4d6e8c;">{E(place)}</div>"""
                : string.Empty;

            cards.Append($"""
                <tr><td style="padding:6px 28px;">
                  <a href="{E(listing.Url)}" style="text-decoration:none;color:#0b1620;display:block;">
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border:1px solid #e5edf3;border-radius:12px;">
                      <tr>
                        <td width="168" valign="top" style="padding:12px;">{photo}</td>
                        <td valign="top" style="padding:12px 14px 12px 0;">
                          <div style="font-size:18px;font-weight:700;color:#0b1620;">{E(Price(listing))}</div>
                          <div style="margin-top:4px;font-size:15px;line-height:1.35;color:#101f2e;">{E(listing.Title)}</div>
                          <div style="margin-top:6px;font-size:13px;color:#4d6e8c;">{E(Facts(listing))}</div>
                          {location}
                        </td>
                      </tr>
                    </table>
                  </a>
                </td></tr>
                """);
        }

        var moreRow = more > 0
            ? $"""<tr><td style="padding:6px 28px 0;font-size:14px;color:#4d6e8c;">…și încă {more}.</td></tr>"""
            : string.Empty;

        var content =
            EmailLayout.Heading($"{headline} pentru „{searchName}”")
            + """<tr><td style="padding:0 28px 14px;font-size:14px;color:#4d6e8c;">Au apărut anunțuri noi pentru căutarea ta salvată.</td></tr>"""
            + cards
            + moreRow
            + EmailLayout.Button("Vezi toate rezultatele", openUrl);
        var footer =
            $"Primești acest email pentru că ai salvat căutarea „{E(searchName)}” pe IMOVA, cu alerte pe email. " +
            $"""<a href="{E(unsubscribeUrl)}" style="color:#4d6e8c;">Oprește alertele pentru această căutare</a>.""";

        return EmailLayout.Page(headline, content, footer);
    }
}
