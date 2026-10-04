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
        ListingEmailText.Price(listing.Amount, listing.Currency, listing.TransactionType);

    // "Garaj · Vânzare · 18 m²"
    public static string Facts(AlertListing listing) =>
        ListingEmailText.Facts(listing.PropertyType, listing.TransactionType, listing.AreaM2);

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
            cards.Append(new ListingEmailCard(
                    listing.Title, listing.Url, listing.PhotoUrl, $"""<span style="white-space:nowrap;">{E(Price(listing))}</span>""", Facts(listing), listing.Location)
                .CompactHtml());
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
