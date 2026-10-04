using System.Text;
using Imova.Application.Common.Emails;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;

namespace Imova.Application.Features.Favorites.Alerts;

// The two emails about a listing the user saved: its price changed, or it is no longer available.
// HTML (the listing as a card, like on the site — ListingEmailCard) + the same content as plain
// text, Romanian like the other emails, built on EmailLayout.
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

    public static EmailMessage PriceChanged(string to, PriceChange change, string listingUrl, string unsubscribeUrl, string settingsUrl)
    {
        var dropped = change.OldCurrency == change.NewCurrency && change.NewAmount < change.OldAmount;
        var headline = dropped ? "Prețul a scăzut" : "Prețul s-a schimbat";
        var subject = $"{headline}: {change.Title} — IMOVA";
        var oldPrice = ListingEmailText.Price(change.OldAmount, change.OldCurrency, change.TransactionType);
        var newPrice = ListingEmailText.Price(change.NewAmount, change.NewCurrency, change.TransactionType);

        var percent = ChangePercent(change);
        var facts = ListingEmailText.Facts(change.PropertyType, change.TransactionType, change.AreaM2);

        var text = new StringBuilder()
            .Append($"Anunțul pe care l-ai salvat și-a schimbat prețul:\n\n{change.Title}\n{facts}\n")
            .Append(change.Location is null ? string.Empty : $"{change.Location}\n")
            .Append($"Preț vechi: {oldPrice}\nPreț nou: {newPrice}{(percent is null ? string.Empty : $" ({percent})")}\n\n")
            .Append($"Vezi anunțul: {listingUrl}\n\n")
            .Append(TextFooter(unsubscribeUrl, settingsUrl))
            .ToString();

        static string E(string value) => EmailLayout.Encode(value);
        // New price (green when it dropped), the old one struck through, and the change as a pill.
        var newColor = dropped ? "#1f8a4c" : "#0b1620";
        var percentPill = percent is null
            ? string.Empty
            : $"""<span style="display:inline-block;margin-left:6px;padding:1px 8px;border-radius:999px;background:{(dropped ? "#e3f4ea" : "#eef2f6")};color:{newColor};font-size:12px;font-weight:700;vertical-align:middle;">{E(percent)}</span>""";
        var priceHtml =
            $"""<span style="color:{newColor};white-space:nowrap;">{E(newPrice)}</span>{percentPill}"""
            + $"""<div style="margin-top:2px;font-size:13px;font-weight:400;color:#7592ac;">înainte <span style="text-decoration:line-through;white-space:nowrap;">{E(oldPrice)}</span></div>""";

        var content =
            EmailLayout.Heading(headline)
            + EmailLayout.Paragraph("Anunțul pe care l-ai salvat și-a schimbat prețul.")
            + new ListingEmailCard(change.Title, listingUrl, change.PhotoUrl, priceHtml, facts, change.Location).LargeHtml()
            + EmailLayout.Button("Vezi anunțul", listingUrl);

        return new EmailMessage(to, subject, text, EmailLayout.Page(headline, content, HtmlFooter(unsubscribeUrl, settingsUrl)));
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
        string? photoUrl = null)
    {
        var headline = "Anunțul salvat nu mai este disponibil";
        var subject = $"Nu mai este disponibil: {ended.Title} — IMOVA";
        var reason = Reason(ended.Status);
        var facts = ListingEmailText.Facts(ended.PropertyType, ended.TransactionType, ended.TotalAreaM2);
        var location = ListingEmailText.Location(ended.RaionName, ended.LocalitateName, ended.ChisinauSectorName);
        var lastPrice = ListingEmailText.Price(ended.Price.Amount, ended.Price.Currency, ended.TransactionType);

        var text = new StringBuilder()
            .Append($"Un anunț pe care l-ai salvat nu mai este disponibil. {reason}\n\n")
            .Append($"{ended.Title}\n{facts}\n")
            .Append(location is null ? string.Empty : $"{location}\n")
            .Append($"Ultimul preț: {lastPrice}\n\n")
            .Append($"Vezi anunțuri asemănătoare: {similarUrl}\n")
            .Append($"Caută în aceeași zonă: {searchUrl}\n\n")
            .Append(TextFooter(unsubscribeUrl, settingsUrl))
            .ToString();

        static string E(string value) => EmailLayout.Encode(value);
        var priceHtml =
            $"""<span style="font-size:13px;font-weight:400;color:#4d6e8c;">Ultimul preț</span><br><span style="color:#4d6e8c;white-space:nowrap;">{E(lastPrice)}</span>""";
        var content =
            EmailLayout.Heading(headline)
            + EmailLayout.Paragraph($"Un anunț pe care l-ai salvat nu mai este disponibil. {E(reason)}")
            + new ListingEmailCard(ended.Title, listingUrl, photoUrl, priceHtml, facts, location, Badge: StatusLabel(ended.Status)).LargeHtml()
            + EmailLayout.Button("Vezi anunțuri asemănătoare", similarUrl)
            + $"""<tr><td align="center" style="padding:0 28px 8px;font-size:14px;"><a href="{E(searchUrl)}" style="color:#4d6e8c;">Caută în aceeași zonă</a></td></tr>""";

        return new EmailMessage(to, subject, text, EmailLayout.Page(headline, content, HtmlFooter(unsubscribeUrl, settingsUrl)));
    }

    // The 410 page's badge (EndedListing.status.* in the web app's ro.json).
    public static string StatusLabel(string status) => status switch
    {
        "Sold" => "Vândut",
        "Rented" => "Închiriat",
        "Expired" => "Expirat",
        _ => "Retras",
    };

    // The 410 page's own words (EndedListing.reason.* in the web app's ro.json).
    public static string Reason(string status) => status switch
    {
        "Sold" => "Proprietatea a fost vândută.",
        "Rented" => "Proprietatea a fost închiriată.",
        "Expired" => "Anunțul a expirat și nu a fost prelungit.",
        _ => "Autorul a retras anunțul.",
    };

    private static string TextFooter(string unsubscribeUrl, string settingsUrl) =>
        "Primești acest email pentru că ai salvat anunțul la favorite pe IMOVA.\n"
        + $"Nu mai vrei emailuri despre anunțurile salvate? {unsubscribeUrl}\n"
        + $"Setările notificărilor: {settingsUrl}\n";

    private static string HtmlFooter(string unsubscribeUrl, string settingsUrl) =>
        "Primești acest email pentru că ai salvat anunțul la favorite pe IMOVA. "
        + $"""<a href="{EmailLayout.Encode(unsubscribeUrl)}" style="color:#4d6e8c;">Nu mai trimite emailuri despre anunțurile salvate</a> · """
        + $"""<a href="{EmailLayout.Encode(settingsUrl)}" style="color:#4d6e8c;">Setările notificărilor</a>""";
}
