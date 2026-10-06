using Imova.Application.Common;
using Imova.Application.Common.Emails;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Agencies;

namespace Imova.Application.Features.Agencies.Invitations;

// The invitation email (Romanian, HTML + text), with the link to /invitations/{token}.
public class AgencyInvitationEmail(IEmailSender emailSender, AppOptions appOptions)
{
    public Task SendAsync(
        string to, string inviterName, string agencyName, AgencyRole role, string token, CancellationToken cancellationToken) =>
        emailSender.SendAsync(Build(to, inviterName, agencyName, role, appOptions.WebUrl($"/invitations/{token}")), cancellationToken);

    public static EmailMessage Build(string to, string inviterName, string agencyName, AgencyRole role, string link)
    {
        var heading = $"{inviterName} te-a invitat să te alături agenției {agencyName} pe IMOVA";
        var asRole = role == AgencyRole.Admin
            ? "Vei fi administrator: vei putea edita profilul agenției, invita agenți și gestiona toate anunțurile ei."
            : "Vei fi agent: vei putea publica anunțuri în numele agenției.";
        const string expiry = "Invitația este valabilă 7 zile. Dacă nu te așteptai la ea, poți ignora acest email.";

        var text = $"{heading}.\n\n{asRole}\n\nAcceptă invitația: {link}\n\n{expiry}\n";
        var html = EmailLayout.Page(
            "Invitație într-o agenție",
            EmailLayout.Heading(heading)
            + EmailLayout.Paragraph(EmailLayout.Encode(asRole))
            + EmailLayout.Button("Vezi invitația", link),
            EmailLayout.Encode(expiry));
        return new EmailMessage(to, $"Invitație: agenția {agencyName} — IMOVA", text, html);
    }
}
