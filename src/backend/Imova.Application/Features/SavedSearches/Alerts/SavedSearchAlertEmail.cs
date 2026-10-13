using System.Text;
using Imova.Application.Common;
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
// plain text for clients that don't show HTML. In the user's language. Email HTML is its own world:
// tables and inline styles only, fixed-size images, no external CSS or scripts.
// Every user-written value (titles, the search name) is HTML-encoded.
public static class SavedSearchAlertEmail
{
    public static EmailMessage Build(
        string to, string searchName, IReadOnlyList<AlertListing> listings, int total, string openUrl, string unsubscribeUrl, string language)
    {
        var headline = Headline(total, language);
        var subject = Languages.Pick(
            language,
            $"{headline} pentru „{searchName}” — IMOVA",
            $"{headline} for “{searchName}” — IMOVA",
            $"{headline} по запросу «{searchName}» — IMOVA");
        var more = total - listings.Count;

        return new EmailMessage(
            to,
            subject,
            Text(searchName, listings, more, openUrl, unsubscribeUrl, language),
            Html(headline, searchName, listings, more, openUrl, unsubscribeUrl, language));
    }

    // "Un anunț nou" / "12 anunțuri noi" / "20 de anunțuri noi"; "1 new listing"; "1 новое объявление" /
    // "3 новых объявления" / "5 новых объявлений".
    public static string Headline(int total, string language) => language switch
    {
        Languages.English => total == 1 ? "1 new listing" : $"{total} new listings",
        Languages.Russian => $"{total} " + (total % 10, total % 100) switch
        {
            (1, not 11) => "новое объявление",
            (2 or 3 or 4, not (12 or 13 or 14)) => "новых объявления",
            _ => "новых объявлений",
        },
        // Romanian puts "de" before a number from 20 on whose last two digits aren't 01–19.
        _ => total == 1 ? "Un anunț nou" : total >= 20 && total % 100 is 0 or >= 20 ? $"{total} de anunțuri noi" : $"{total} anunțuri noi",
    };

    // "9 000 EUR" / "450 EUR / lună"
    public static string Price(AlertListing listing, string language) =>
        ListingEmailText.Price(listing.Amount, listing.Currency, listing.TransactionType, language);

    // "Garaj · Vânzare · 18 m²"
    public static string Facts(AlertListing listing, string language) =>
        ListingEmailText.Facts(listing.PropertyType, listing.TransactionType, listing.AreaM2, language);

    private static string More(int more, string language) =>
        Languages.Pick(language, $"…și încă {more}.", $"…and {more} more.", $"…и ещё {more}.");

    private static string SeeAll(string language) =>
        Languages.Pick(language, "Vezi toate rezultatele", "View all results", "Посмотреть все результаты");

    private static string Text(
        string searchName, IReadOnlyList<AlertListing> listings, int more, string openUrl, string unsubscribeUrl, string language)
    {
        string T(string ro, string en, string ru) => Languages.Pick(language, ro, en, ru);

        var text = new StringBuilder();
        text.Append(T(
            $"Au apărut anunțuri noi pentru căutarea ta salvată „{searchName}”:\n\n",
            $"There are new listings for your saved search “{searchName}”:\n\n",
            $"Появились новые объявления по вашему сохранённому поиску «{searchName}»:\n\n"));
        foreach (var listing in listings)
        {
            text.Append($"• {listing.Title} — {Price(listing, language)}\n  {Facts(listing, language)}");
            if (listing.Location is { } location) text.Append($" · {location}");
            text.Append($"\n  {listing.Url}\n\n");
        }

        if (more > 0) text.Append($"{More(more, language)}\n\n");
        text.Append($"{SeeAll(language)}: {openUrl}\n\n");
        text.Append(T(
            $"Nu mai vrei aceste emailuri? Oprește alertele pentru această căutare:\n{unsubscribeUrl}\n",
            $"Don't want these emails any more? Turn off alerts for this search:\n{unsubscribeUrl}\n",
            $"Больше не хотите получать эти письма? Отключите уведомления для этого поиска:\n{unsubscribeUrl}\n"));
        return text.ToString();
    }

    private static string Html(
        string headline, string searchName, IReadOnlyList<AlertListing> listings, int more, string openUrl, string unsubscribeUrl, string language)
    {
        string T(string ro, string en, string ru) => Languages.Pick(language, ro, en, ru);
        static string E(string value) => EmailLayout.Encode(value);

        var cards = new StringBuilder();
        foreach (var listing in listings)
        {
            cards.Append(new ListingEmailCard(
                    listing.Title,
                    listing.Url,
                    listing.PhotoUrl,
                    $"""<span style="white-space:nowrap;">{E(Price(listing, language))}</span>""",
                    Facts(listing, language),
                    listing.Location)
                .CompactHtml(language));
        }

        var moreRow = more > 0
            ? $"""<tr><td style="padding:6px 28px 0;font-size:14px;color:#4d6e8c;">{E(More(more, language))}</td></tr>"""
            : string.Empty;

        var intro = T(
            "Au apărut anunțuri noi pentru căutarea ta salvată.",
            "There are new listings for your saved search.",
            "Появились новые объявления по вашему сохранённому поиску.");
        var content =
            EmailLayout.Heading(T($"{headline} pentru „{searchName}”", $"{headline} for “{searchName}”", $"{headline} по запросу «{searchName}»"))
            + $"""<tr><td style="padding:0 28px 14px;font-size:14px;color:#4d6e8c;">{E(intro)}</td></tr>"""
            + cards
            + moreRow
            + EmailLayout.Button(SeeAll(language), openUrl);
        var why = T(
            $"Primești acest email pentru că ai salvat căutarea „{searchName}” pe IMOVA, cu alerte pe email.",
            $"You're getting this email because you saved the search “{searchName}” on IMOVA, with email alerts.",
            $"Вы получили это письмо, потому что сохранили поиск «{searchName}» на IMOVA с уведомлениями по почте.");
        var stop = T(
            "Oprește alertele pentru această căutare",
            "Turn off alerts for this search",
            "Отключить уведомления для этого поиска");
        var footer = $"""{E(why)} <a href="{E(unsubscribeUrl)}" style="color:#4d6e8c;">{E(stop)}</a>.""";

        return EmailLayout.Page(headline, content, footer, language);
    }
}
