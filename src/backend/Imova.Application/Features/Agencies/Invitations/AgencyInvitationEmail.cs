using Imova.Application.Common;
using Imova.Application.Common.Emails;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Agencies;

namespace Imova.Application.Features.Agencies.Invitations;

// The invitation email (HTML + text), with the link to /invitations/{token}. Its language is chosen
// by AgencyInvitationRules.LanguageAsync: the invitee's, else the inviter's.
public class AgencyInvitationEmail(IEmailSender emailSender, AppOptions appOptions)
{
    public Task SendAsync(
        string to, string inviterName, string agencyName, AgencyRole role, string token, string language, CancellationToken cancellationToken) =>
        emailSender.SendAsync(
            Build(to, inviterName, agencyName, role, appOptions.WebUrl($"/invitations/{token}"), language), cancellationToken);

    public static EmailMessage Build(string to, string inviterName, string agencyName, AgencyRole role, string link, string language)
    {
        string T(string ro, string en, string ru) => Languages.Pick(language, ro, en, ru);

        var heading = T(
            $"{inviterName} te-a invitat să te alături agenției {agencyName} pe IMOVA",
            $"{inviterName} invited you to join the agency {agencyName} on IMOVA",
            $"{inviterName} приглашает вас присоединиться к агентству {agencyName} на IMOVA");
        var asRole = role == AgencyRole.Admin
            ? T(
                "Vei fi administrator: vei putea edita profilul agenției, invita agenți și gestiona toate anunțurile ei.",
                "You'll be an administrator: you'll be able to edit the agency's profile, invite agents and manage all its listings.",
                "Вы будете администратором: сможете редактировать профиль агентства, приглашать агентов и управлять всеми его объявлениями.")
            : T(
                "Vei fi agent: vei putea publica anunțuri în numele agenției.",
                "You'll be an agent: you'll be able to publish listings on the agency's behalf.",
                "Вы будете агентом: сможете публиковать объявления от имени агентства.");
        var expiry = T(
            "Invitația este valabilă 7 zile. Dacă nu te așteptai la ea, poți ignora acest email.",
            "The invitation is valid for 7 days. If you weren't expecting it, you can ignore this email.",
            "Приглашение действительно 7 дней. Если вы его не ожидали, просто проигнорируйте это письмо.");
        var accept = T("Acceptă invitația", "Accept the invitation", "Принять приглашение");

        var text = $"{heading}.\n\n{asRole}\n\n{accept}: {link}\n\n{expiry}\n";
        var html = EmailLayout.Page(
            T("Invitație într-o agenție", "Invitation to an agency", "Приглашение в агентство"),
            EmailLayout.Heading(heading)
            + EmailLayout.Paragraph(EmailLayout.Encode(asRole))
            + EmailLayout.Button(T("Vezi invitația", "See the invitation", "Посмотреть приглашение"), link),
            EmailLayout.Encode(expiry),
            language);
        return new EmailMessage(
            to,
            T($"Invitație: agenția {agencyName} — IMOVA", $"Invitation: the agency {agencyName} — IMOVA", $"Приглашение: агентство {agencyName} — IMOVA"),
            text,
            html);
    }
}
