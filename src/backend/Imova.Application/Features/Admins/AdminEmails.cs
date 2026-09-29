using Imova.Application.Common;
using Imova.Application.Common.Emails;
using Imova.Application.Common.Interfaces;

namespace Imova.Application.Features.Admins;

// Who is an admin is never changed silently: the new admin is told, and so is every admin — so a
// hijacked admin account adding someone gets noticed by the others.
public class AdminEmails(IEmailSender emailSender, AppOptions appOptions)
{
    public Task SendWelcomeAsync(string to, string grantedBy, CancellationToken cancellationToken) =>
        emailSender.SendAsync(WelcomeEmail(to, grantedBy, appOptions.WebUrl("/admin/admins")), cancellationToken);

    public Task SendNoticeAsync(string to, string newAdmin, string grantedBy, CancellationToken cancellationToken) =>
        emailSender.SendAsync(NoticeEmail(to, newAdmin, grantedBy, appOptions.WebUrl("/admin/admins")), cancellationToken);

    public static EmailMessage WelcomeEmail(string to, string grantedBy, string link)
    {
        var body = $"Contul tău IMOVA a primit drepturi de administrator, acordate de {grantedBy}.";
        const string notYou = "Dacă nu te așteptai la asta, anunță imediat echipa IMOVA.";
        var text = $"{body}\n\nAdministratori: {link}\n\n{notYou}\n";
        var html = EmailLayout.Page(
            "Ești administrator IMOVA",
            EmailLayout.Heading("Ești administrator IMOVA")
            + EmailLayout.Paragraph(EmailLayout.Encode(body))
            + EmailLayout.Button("Vezi administratorii", link),
            EmailLayout.Encode(notYou));
        return new EmailMessage(to, "Ai primit drepturi de administrator — IMOVA", text, html);
    }

    public static EmailMessage NoticeEmail(string to, string newAdmin, string grantedBy, string link)
    {
        var body = $"{newAdmin} a primit drepturi de administrator pe IMOVA, acordate de {grantedBy}.";
        const string check = "Dacă această schimbare nu era plănuită, verifică imediat lista administratorilor.";
        var text = $"{body}\n\n{check}\n{link}\n";
        var html = EmailLayout.Page(
            "Administrator nou",
            EmailLayout.Heading("Administrator nou pe IMOVA")
            + EmailLayout.Paragraph(EmailLayout.Encode(body))
            + EmailLayout.Paragraph(EmailLayout.Encode(check))
            + EmailLayout.Button("Vezi administratorii", link),
            EmailLayout.Encode("Primești acest email pentru că ești administrator IMOVA."));
        return new EmailMessage(to, $"Administrator nou: {newAdmin} — IMOVA", text, html);
    }
}
