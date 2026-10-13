using Imova.Application.Common;
using Imova.Application.Common.Emails;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Listings;

namespace Imova.Application.Features.Listings.Expiry;

// The two emails of the expiry job, both to the listing's owner and both pointing at "Anunțurile
// mele", where the listing can be renewed (still Active) or re-activated (Expired). In the owner's
// language, in the shared EmailLayout look, with a plain-text version.
public static class ListingExpiryEmails
{
    // Dates in emails are Moldovan calendar dates; falls back to UTC if the zone isn't installed.
    private static readonly TimeZoneInfo Moldova = FindMoldovaTimeZone();

    private static string Period(string language) =>
        Languages.Pick(language, $"{Listing.ActiveMonths} luni", $"{Listing.ActiveMonths} months", $"{Listing.ActiveMonths} месяцев");

    private static string Footer(string language) => Languages.Pick(
        language,
        "Primești acest email pentru că ai publicat acest anunț pe IMOVA. " +
        $"Anunțurile rămân active câte {Period(language)}, ca rezultatele căutărilor să fie la zi.",
        "You're getting this email because you published this listing on IMOVA. " +
        $"Listings stay active for {Period(language)} at a time, so search results stay up to date.",
        "Вы получили это письмо, потому что опубликовали это объявление на IMOVA. " +
        $"Объявления остаются активными {Period(language)}, чтобы результаты поиска были актуальными.");

    private static string MyListings(string language) => Languages.Pick(language, "Anunțurile mele", "My listings", "Мои объявления");

    public static EmailMessage Reminder(string to, string title, DateTimeOffset expiresAt, string myListingsUrl, string language)
    {
        string T(string ro, string en, string ru) => Languages.Pick(language, ro, en, ru);

        var date = FormatDate(expiresAt);
        var period = Period(language);
        var subject = T(
            $"Anunțul tău expiră pe {date} — IMOVA",
            $"Your listing expires on {date} — IMOVA",
            $"Срок действия вашего объявления истекает {date} — IMOVA");
        var text = T(
            $"Anunțul tău „{title}” expiră pe {date}. După această dată nu mai apare în căutări.\n\n" +
            $"Dacă este încă disponibil, prelungește-l cu încă {period} din „{MyListings(language)}”:\n{myListingsUrl}\n",
            $"Your listing “{title}” expires on {date}. After that date it no longer shows up in searches.\n\n" +
            $"If it's still available, renew it for another {period} from “{MyListings(language)}”:\n{myListingsUrl}\n",
            $"Срок действия вашего объявления «{title}» истекает {date}. После этой даты оно больше не будет показываться в поиске.\n\n" +
            $"Если оно ещё актуально, продлите его ещё на {period} в разделе «{MyListings(language)}»:\n{myListingsUrl}\n");

        var encodedTitle = EmailLayout.Encode(title);
        var content =
            EmailLayout.Heading(T("Anunțul tău expiră în curând", "Your listing expires soon", "Срок действия вашего объявления скоро истекает"))
            + EmailLayout.Paragraph(T(
                $"Anunțul <strong>„{encodedTitle}”</strong> expiră pe <strong>{date}</strong>. După această dată nu mai apare în căutări.",
                $"The listing <strong>“{encodedTitle}”</strong> expires on <strong>{date}</strong>. After that date it no longer shows up in searches.",
                $"Срок действия объявления <strong>«{encodedTitle}»</strong> истекает <strong>{date}</strong>. После этой даты оно больше не будет показываться в поиске."))
            + EmailLayout.Paragraph(EmailLayout.Encode(T(
                $"Dacă este încă disponibil, prelungește-l cu încă {period} — durează un click.",
                $"If it's still available, renew it for another {period} — it takes one click.",
                $"Если оно ещё актуально, продлите его ещё на {period} — это один клик.")))
            + EmailLayout.Button(T("Prelungește anunțul", "Renew listing", "Продлить объявление"), myListingsUrl);

        return new EmailMessage(to, subject, text, EmailLayout.Page(subject, content, Footer(language), language));
    }

    public static EmailMessage Expired(string to, string title, string myListingsUrl, string language)
    {
        string T(string ro, string en, string ru) => Languages.Pick(language, ro, en, ru);

        var subject = T("Anunțul tău a expirat — IMOVA", "Your listing has expired — IMOVA", "Срок действия вашего объявления истёк — IMOVA");
        var text = T(
            $"Anunțul tău „{title}” a expirat și nu mai apare în căutări.\n\n" +
            $"Dacă este încă disponibil, îl poți reactiva din „{MyListings(language)}”, fără o nouă verificare:\n{myListingsUrl}\n",
            $"Your listing “{title}” has expired and no longer shows up in searches.\n\n" +
            $"If it's still available, you can reactivate it from “{MyListings(language)}”, with no new review:\n{myListingsUrl}\n",
            $"Срок действия вашего объявления «{title}» истёк, и оно больше не показывается в поиске.\n\n" +
            $"Если оно ещё актуально, его можно снова активировать в разделе «{MyListings(language)}» без повторной проверки:\n{myListingsUrl}\n");

        var encodedTitle = EmailLayout.Encode(title);
        var content =
            EmailLayout.Heading(T("Anunțul tău a expirat", "Your listing has expired", "Срок действия вашего объявления истёк"))
            + EmailLayout.Paragraph(T(
                $"Anunțul <strong>„{encodedTitle}”</strong> a expirat și nu mai apare în căutări.",
                $"The listing <strong>“{encodedTitle}”</strong> has expired and no longer shows up in searches.",
                $"Срок действия объявления <strong>«{encodedTitle}»</strong> истёк, и оно больше не показывается в поиске."))
            + EmailLayout.Paragraph(EmailLayout.Encode(T(
                "Dacă este încă disponibil, îl poți reactiva oricând, fără o nouă verificare.",
                "If it's still available, you can reactivate it at any time, with no new review.",
                "Если оно ещё актуально, его можно снова активировать в любой момент без повторной проверки.")))
            + EmailLayout.Button(T("Reactivează anunțul", "Reactivate listing", "Активировать объявление"), myListingsUrl);

        return new EmailMessage(to, subject, text, EmailLayout.Page(subject, content, Footer(language), language));
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
