using Imova.Application.Common.Emails;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Listings;

namespace Imova.Application.Features.Listings.Expiry;

// The two emails of the expiry job, both to the listing's owner and both pointing at "Anunțurile
// mele", where the listing can be renewed (still Active) or re-activated (Expired). Romanian, in
// the shared EmailLayout look, with a plain-text version.
public static class ListingExpiryEmails
{
    // Dates in emails are Moldovan calendar dates; falls back to UTC if the zone isn't installed.
    private static readonly TimeZoneInfo Moldova = FindMoldovaTimeZone();

    private static readonly string Period = $"{Listing.ActiveMonths} luni";

    private static readonly string Footer =
        "Primești acest email pentru că ai publicat acest anunț pe IMOVA. " +
        $"Anunțurile rămân active câte {Period}, ca rezultatele căutărilor să fie la zi.";

    public static EmailMessage Reminder(string to, string title, DateTimeOffset expiresAt, string myListingsUrl)
    {
        var date = FormatDate(expiresAt);
        var subject = $"Anunțul tău expiră pe {date} — IMOVA";
        var text =
            $"Anunțul tău „{title}” expiră pe {date}. După această dată nu mai apare în căutări.\n\n" +
            $"Dacă este încă disponibil, prelungește-l cu încă {Period} din „Anunțurile mele”:\n{myListingsUrl}\n";

        var content =
            EmailLayout.Heading("Anunțul tău expiră în curând")
            + EmailLayout.Paragraph(
                $"Anunțul <strong>„{EmailLayout.Encode(title)}”</strong> expiră pe <strong>{date}</strong>. " +
                "După această dată nu mai apare în căutări.")
            + EmailLayout.Paragraph($"Dacă este încă disponibil, prelungește-l cu încă {Period} — durează un click.")
            + EmailLayout.Button("Prelungește anunțul", myListingsUrl);

        return new EmailMessage(to, subject, text, EmailLayout.Page(subject, content, Footer));
    }

    public static EmailMessage Expired(string to, string title, string myListingsUrl)
    {
        const string subject = "Anunțul tău a expirat — IMOVA";
        var text =
            $"Anunțul tău „{title}” a expirat și nu mai apare în căutări.\n\n" +
            $"Dacă este încă disponibil, îl poți reactiva din „Anunțurile mele”, fără o nouă verificare:\n{myListingsUrl}\n";

        var content =
            EmailLayout.Heading("Anunțul tău a expirat")
            + EmailLayout.Paragraph(
                $"Anunțul <strong>„{EmailLayout.Encode(title)}”</strong> a expirat și nu mai apare în căutări.")
            + EmailLayout.Paragraph("Dacă este încă disponibil, îl poți reactiva oricând, fără o nouă verificare.")
            + EmailLayout.Button("Reactivează anunțul", myListingsUrl);

        return new EmailMessage(to, subject, text, EmailLayout.Page(subject, content, Footer));
    }

    public static string FormatDate(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, Moldova).ToString("dd.MM.yyyy");

    private static TimeZoneInfo FindMoldovaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Chisinau");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
