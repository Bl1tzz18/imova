using Imova.Application.Common;
using Imova.Application.Common.Emails;
using Imova.Application.Common.Interfaces;

namespace Imova.Application.Features.Admins;

// Who is an admin is never changed silently: the new admin is told, and so is every admin — so a
// hijacked admin account adding someone gets noticed by the others. Each in the recipient's language.
public class AdminEmails(IEmailSender emailSender, AppOptions appOptions)
{
    public Task SendWelcomeAsync(string to, string grantedBy, string language, CancellationToken cancellationToken) =>
        emailSender.SendAsync(WelcomeEmail(to, grantedBy, appOptions.WebUrl("/admin/admins"), language), cancellationToken);

    public Task SendNoticeAsync(string to, string newAdmin, string grantedBy, string language, CancellationToken cancellationToken) =>
        emailSender.SendAsync(NoticeEmail(to, newAdmin, grantedBy, appOptions.WebUrl("/admin/admins"), language), cancellationToken);

    public static EmailMessage WelcomeEmail(string to, string grantedBy, string link, string language)
    {
        string T(string ro, string en, string ru) => Languages.Pick(language, ro, en, ru);

        var title = T("Ești administrator IMOVA", "You're an IMOVA administrator", "Вы администратор IMOVA");
        var body = T(
            $"Contul tău IMOVA a primit drepturi de administrator, acordate de {grantedBy}.",
            $"Your IMOVA account has been given administrator rights by {grantedBy}.",
            $"Вашему аккаунту IMOVA предоставлены права администратора. Права выдал администратор {grantedBy}.");
        var notYou = T(
            "Dacă nu te așteptai la asta, anunță imediat echipa IMOVA.",
            "If you weren't expecting this, tell the IMOVA team right away.",
            "Если вы этого не ожидали, немедленно сообщите команде IMOVA.");
        var admins = T("Administratori", "Administrators", "Администраторы");
        var button = T("Vezi administratorii", "See the administrators", "Посмотреть администраторов");

        var text = $"{body}\n\n{admins}: {link}\n\n{notYou}\n";
        var html = EmailLayout.Page(
            title,
            EmailLayout.Heading(title)
            + EmailLayout.Paragraph(EmailLayout.Encode(body))
            + EmailLayout.Button(button, link),
            EmailLayout.Encode(notYou),
            language);
        return new EmailMessage(
            to,
            T("Ai primit drepturi de administrator — IMOVA", "You've been made an administrator — IMOVA", "Вам выданы права администратора — IMOVA"),
            text,
            html);
    }

    public static EmailMessage NoticeEmail(string to, string newAdmin, string grantedBy, string link, string language)
    {
        string T(string ro, string en, string ru) => Languages.Pick(language, ro, en, ru);

        var body = T(
            $"{newAdmin} a primit drepturi de administrator pe IMOVA, acordate de {grantedBy}.",
            $"{newAdmin} has been given administrator rights on IMOVA by {grantedBy}.",
            $"{newAdmin} — новый администратор IMOVA. Права выдал администратор {grantedBy}.");
        var check = T(
            "Dacă această schimbare nu era plănuită, verifică imediat lista administratorilor.",
            "If this change wasn't planned, check the list of administrators right away.",
            "Если это изменение не планировалось, немедленно проверьте список администраторов.");
        var footer = T(
            "Primești acest email pentru că ești administrator IMOVA.",
            "You're getting this email because you're an IMOVA administrator.",
            "Вы получили это письмо, потому что вы администратор IMOVA.");
        var button = T("Vezi administratorii", "See the administrators", "Посмотреть администраторов");

        var text = $"{body}\n\n{check}\n{link}\n";
        var html = EmailLayout.Page(
            T("Administrator nou", "New administrator", "Новый администратор"),
            EmailLayout.Heading(T("Administrator nou pe IMOVA", "New administrator on IMOVA", "Новый администратор на IMOVA"))
            + EmailLayout.Paragraph(EmailLayout.Encode(body))
            + EmailLayout.Paragraph(EmailLayout.Encode(check))
            + EmailLayout.Button(button, link),
            EmailLayout.Encode(footer),
            language);
        return new EmailMessage(
            to,
            T($"Administrator nou: {newAdmin} — IMOVA", $"New administrator: {newAdmin} — IMOVA", $"Новый администратор: {newAdmin} — IMOVA"),
            text,
            html);
    }
}
