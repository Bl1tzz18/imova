using System.Text;
using Imova.Application.Common;
using Imova.Application.Common.Emails;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;

namespace Imova.Application.Features.Favorites.Alerts;

// The two emails about a listing the user saved: its price changed, or it is no longer available.
// HTML (the listing as a card, like on the site — ListingEmailCard) + the same content as plain
// text, in the user's language, built on EmailLayout.
// Every user-written value (the title) is HTML-encoded.
public static class FavoriteAlertEmails
{
    // What the price email says, and what its listing card shows (photo, kind/size, place).
    public record PriceChange(
        Guid ListingId,
        string Title,
        string TransactionType,
        decimal OldAmount,
        string OldCurrency,
        decimal NewAmount,
        string NewCurrency,
        string PropertyType = "",
        decimal AreaM2 = 0,
        string? Location = null,
        string? PhotoUrl = null);

    // "−5%" / "+3%" — only when the currency stayed the same (else the numbers don't compare).
    public static string? ChangePercent(PriceChange change)
    {
        if (change.OldCurrency != change.NewCurrency || change.OldAmount <= 0)
        {
            return null;
        }

        var percent = (int)Math.Round((change.NewAmount - change.OldAmount) / change.OldAmount * 100m, MidpointRounding.AwayFromZero);
        return percent switch
        {
            0 => null,
            < 0 => $"−{-percent}%",
            _ => $"+{percent}%",
        };
    }

    public static EmailMessage PriceChanged(
        string to, PriceChange change, string listingUrl, string unsubscribeUrl, string settingsUrl, string language)
    {
        string T(string ro, string en, string ru) => Languages.Pick(language, ro, en, ru);

        var dropped = change.OldCurrency == change.NewCurrency && change.NewAmount < change.OldAmount;
        var headline = dropped
            ? T("Prețul a scăzut", "The price has dropped", "Цена снизилась")
            : T("Prețul s-a schimbat", "The price has changed", "Цена изменилась");
        var subject = $"{headline}: {change.Title} — IMOVA";
        var oldPrice = ListingEmailText.Price(change.OldAmount, change.OldCurrency, change.TransactionType, language);
        var newPrice = ListingEmailText.Price(change.NewAmount, change.NewCurrency, change.TransactionType, language);

        var percent = ChangePercent(change);
        var facts = ListingEmailText.Facts(change.PropertyType, change.TransactionType, change.AreaM2, language);
        var intro = T(
            "Anunțul pe care l-ai salvat și-a schimbat prețul.",
            "A listing you saved has changed its price.",
            "У сохранённого вами объявления изменилась цена.");
        var view = T("Vezi anunțul", "See the listing", "Посмотреть объявление");

        var text = new StringBuilder()
            .Append($"{intro.TrimEnd('.')}:\n\n{change.Title}\n{facts}\n")
            .Append(change.Location is null ? string.Empty : $"{change.Location}\n")
            .Append($"{T("Preț vechi", "Old price", "Старая цена")}: {oldPrice}\n")
            .Append($"{T("Preț nou", "New price", "Новая цена")}: {newPrice}{(percent is null ? string.Empty : $" ({percent})")}\n\n")
            .Append($"{view}: {listingUrl}\n\n")
            .Append(TextFooter(unsubscribeUrl, settingsUrl, language))
            .ToString();

        static string E(string value) => EmailLayout.Encode(value);
        // New price (green when it dropped), the old one struck through, and the change as a pill.
        var newColor = dropped ? "#1f8a4c" : "#0b1620";
        var percentPill = percent is null
            ? string.Empty
            : $"""<span style="display:inline-block;margin-left:6px;padding:1px 8px;border-radius:999px;background:{(dropped ? "#e3f4ea" : "#eef2f6")};color:{newColor};font-size:12px;font-weight:700;vertical-align:middle;">{E(percent)}</span>""";
        var priceHtml =
            $"""<span style="color:{newColor};white-space:nowrap;">{E(newPrice)}</span>{percentPill}"""
            + $"""<div style="margin-top:2px;font-size:13px;font-weight:400;color:#7592ac;">{E(T("înainte", "before", "было"))} <span style="text-decoration:line-through;white-space:nowrap;">{E(oldPrice)}</span></div>""";

        var content =
            EmailLayout.Heading(headline)
            + EmailLayout.Paragraph(E(intro))
            + new ListingEmailCard(change.Title, listingUrl, change.PhotoUrl, priceHtml, facts, change.Location).LargeHtml()
            + EmailLayout.Button(view, listingUrl);

        return new EmailMessage(
            to, subject, text, EmailLayout.Page(headline, content, HtmlFooter(unsubscribeUrl, settingsUrl, language), language));
    }

    // The 410 page's summary (EndedListingDto): what it was, where, its last price, and why it's gone.
    public static EmailMessage Ended(
        string to,
        EndedListingDto ended,
        string listingUrl,
        string similarUrl,
        string searchUrl,
        string unsubscribeUrl,
        string settingsUrl,
        string language,
        string? photoUrl = null)
    {
        string T(string ro, string en, string ru) => Languages.Pick(language, ro, en, ru);

        var headline = T(
            "Anunțul salvat nu mai este disponibil", "The listing you saved is no longer available", "Сохранённое объявление больше не доступно");
        var subject = T(
            $"Nu mai este disponibil: {ended.Title} — IMOVA",
            $"No longer available: {ended.Title} — IMOVA",
            $"Больше не доступно: {ended.Title} — IMOVA");
        var reason = Reason(ended.Status, language);
        var facts = ListingEmailText.Facts(ended.PropertyType, ended.TransactionType, ended.TotalAreaM2, language);
        var location = ListingEmailText.Location(ended.RaionName, ended.LocalitateName, ended.ChisinauSectorName);
        var lastPrice = ListingEmailText.Price(ended.Price.Amount, ended.Price.Currency, ended.TransactionType, language);
        var intro = T(
            "Un anunț pe care l-ai salvat nu mai este disponibil.",
            "A listing you saved is no longer available.",
            "Объявление, которое вы сохранили, больше не доступно.");
        var lastPriceLabel = T("Ultimul preț", "Last price", "Последняя цена");
        var similar = T("Vezi anunțuri asemănătoare", "See similar listings", "Посмотреть похожие объявления");
        var sameArea = T("Caută în aceeași zonă", "Search in the same area", "Искать в этом же районе");

        var text = new StringBuilder()
            .Append($"{intro} {reason}\n\n")
            .Append($"{ended.Title}\n{facts}\n")
            .Append(location is null ? string.Empty : $"{location}\n")
            .Append($"{lastPriceLabel}: {lastPrice}\n\n")
            .Append($"{similar}: {similarUrl}\n")
            .Append($"{sameArea}: {searchUrl}\n\n")
            .Append(TextFooter(unsubscribeUrl, settingsUrl, language))
            .ToString();

        static string E(string value) => EmailLayout.Encode(value);
        var priceHtml =
            $"""<span style="font-size:13px;font-weight:400;color:#4d6e8c;">{E(lastPriceLabel)}</span><br><span style="color:#4d6e8c;white-space:nowrap;">{E(lastPrice)}</span>""";
        var content =
            EmailLayout.Heading(headline)
            + EmailLayout.Paragraph($"{E(intro)} {E(reason)}")
            + new ListingEmailCard(ended.Title, listingUrl, photoUrl, priceHtml, facts, location, Badge: StatusLabel(ended.Status, language)).LargeHtml()
            + EmailLayout.Button(similar, similarUrl)
            + $"""<tr><td align="center" style="padding:0 28px 8px;font-size:14px;"><a href="{E(searchUrl)}" style="color:#4d6e8c;">{E(sameArea)}</a></td></tr>""";

        return new EmailMessage(
            to, subject, text, EmailLayout.Page(headline, content, HtmlFooter(unsubscribeUrl, settingsUrl, language), language));
    }

    // The 410 page's badge (EndedListing.status.* in the web app's messages).
    public static string StatusLabel(string status, string language) => status switch
    {
        "Sold" => Languages.Pick(language, "Vândut", "Sold", "Продано"),
        "Rented" => Languages.Pick(language, "Închiriat", "Rented", "Сдано"),
        "Expired" => Languages.Pick(language, "Expirat", "Expired", "Истекло"),
        _ => Languages.Pick(language, "Retras", "Withdrawn", "Снято"),
    };

    // The 410 page's own words (EndedListing.reason.* in the web app's messages).
    public static string Reason(string status, string language) => status switch
    {
        "Sold" => Languages.Pick(language, "Proprietatea a fost vândută.", "The property has been sold.", "Недвижимость продана."),
        "Rented" => Languages.Pick(
            language, "Proprietatea a fost închiriată.", "The property has been rented out.", "Недвижимость сдана в аренду."),
        "Expired" => Languages.Pick(
            language,
            "Anunțul a expirat și nu a fost prelungit.",
            "The listing expired and wasn't renewed.",
            "Срок объявления истёк, и оно не было продлено."),
        _ => Languages.Pick(language, "Autorul a retras anunțul.", "The author took the listing down.", "Автор снял объявление."),
    };

    private static string Why(string language) => Languages.Pick(
        language,
        "Primești acest email pentru că ai salvat anunțul la favorite pe IMOVA.",
        "You're getting this email because you saved this listing to your favorites on IMOVA.",
        "Вы получили это письмо, потому что добавили это объявление в избранное на IMOVA.");

    private static string StopLabel(string language) => Languages.Pick(
        language,
        "Nu mai trimite emailuri despre anunțurile salvate",
        "Stop emails about saved listings",
        "Не присылать письма о сохранённых объявлениях");

    private static string SettingsLabel(string language) =>
        Languages.Pick(language, "Setările notificărilor", "Notification settings", "Настройки уведомлений");

    private static string TextFooter(string unsubscribeUrl, string settingsUrl, string language) =>
        $"{Why(language)}\n"
        + Languages.Pick(
            language,
            "Nu mai vrei emailuri despre anunțurile salvate?",
            "Don't want emails about saved listings any more?",
            "Больше не хотите получать письма о сохранённых объявлениях?")
        + $" {unsubscribeUrl}\n"
        + $"{SettingsLabel(language)}: {settingsUrl}\n";

    private static string HtmlFooter(string unsubscribeUrl, string settingsUrl, string language) =>
        $"{EmailLayout.Encode(Why(language))} "
        + $"""<a href="{EmailLayout.Encode(unsubscribeUrl)}" style="color:#4d6e8c;">{EmailLayout.Encode(StopLabel(language))}</a> · """
        + $"""<a href="{EmailLayout.Encode(settingsUrl)}" style="color:#4d6e8c;">{EmailLayout.Encode(SettingsLabel(language))}</a>""";
}
