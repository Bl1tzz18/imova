using Imova.Application.Common;
using Imova.Application.Common.Emails;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Auth;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Account;

// The two emails around deleting an account, in the shared EmailLayout (HTML + the same text as
// plain text), in the user's language like the other account emails:
// - the confirmation link, for an account without a password (signed up with Google) — opening it
//   leads to a page with a final "delete" button, never deletes by itself (mail scanners open links);
// - the notice once the account is gone, so an unexpected deletion doesn't go unnoticed.
public class AccountDeletionEmails(UserManager<ApplicationUser> userManager, IEmailSender emailSender, AppOptions appOptions)
{
    public async Task SendConfirmationLinkAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, AccountDeletion.TokenPurpose);
        var link = appOptions.WebUrl($"/delete-account?userId={user.Id}&token={AccountTokens.Encode(token)}");
        await emailSender.SendAsync(
            ConfirmationEmail(user.Email!, user.DisplayName, link, user.PreferredLanguage), cancellationToken);
    }

    // The account row is gone by now: the caller keeps its address, name and language from before.
    public Task SendDeletedAsync(string email, string? displayName, string language, CancellationToken cancellationToken) =>
        emailSender.SendAsync(DeletedEmail(email, displayName, language), cancellationToken);

    public static EmailMessage ConfirmationEmail(string to, string? displayName, string link, string language)
    {
        string T(string ro, string en, string ru) => Languages.Pick(language, ro, en, ru);

        var greeting = AccountEmails.Greeting(displayName, language);
        var title = T("Confirmă ștergerea contului", "Confirm deleting your account", "Подтвердите удаление аккаунта");
        var intro = T(
            "Am primit o cerere de ștergere a contului tău IMOVA.",
            "We received a request to delete your IMOVA account.",
            "Мы получили запрос на удаление вашего аккаунта IMOVA.");
        var what = T(
            "Ștergerea este definitivă: contul, anunțurile tale (cu fotografiile lor), favoritele și căutările salvate " +
            "vor fi șterse și nu mai pot fi recuperate. Deschide linkul de mai jos și confirmă pe pagină. " +
            "Linkul este valabil 24 de ore.",
            "Deletion is permanent: your account, your listings (with their photos), your favorites and saved searches " +
            "will be deleted and can't be recovered. Open the link below and confirm on the page. " +
            "The link is valid for 24 hours.",
            "Удаление необратимо: аккаунт, ваши объявления (с их фотографиями), избранное и сохранённые поиски " +
            "будут удалены без возможности восстановления. Откройте ссылку ниже и подтвердите на странице. " +
            "Ссылка действительна в течение 24 часов.");
        var ignore = T(
            "Dacă nu tu ai cerut ștergerea, ignoră acest mesaj — contul tău rămâne neschimbat.",
            "If you didn't request this, ignore this email — your account stays as it is.",
            "Если вы не запрашивали удаление, проигнорируйте это письмо — ваш аккаунт останется без изменений.");
        var button = T("Continuă spre ștergerea contului", "Delete my account", "Перейти к удалению аккаунта");

        var text = $"{greeting}\n\n{intro}\n\n{what}\n\n{link}\n\n{ignore}\n";
        var html = EmailLayout.Page(
            title,
            EmailLayout.Heading(greeting)
            + EmailLayout.Paragraph(EmailLayout.Encode(intro))
            + EmailLayout.Paragraph(EmailLayout.Encode(what))
            + EmailLayout.Button(button, link)
            + EmailLayout.LinkFallback(link, language),
            EmailLayout.Encode(ignore),
            language);

        return new EmailMessage(to, $"{title} — IMOVA", text, html);
    }

    public static EmailMessage DeletedEmail(string to, string? displayName, string language)
    {
        string T(string ro, string en, string ru) => Languages.Pick(language, ro, en, ru);

        var greeting = AccountEmails.Greeting(displayName, language);
        var title = T("Contul tău a fost șters", "Your account has been deleted", "Ваш аккаунт удалён");
        var done = T(
            "Contul tău IMOVA a fost șters, împreună cu anunțurile, fotografiile, favoritele și căutările salvate. " +
            "Nu vei mai primi emailuri de la noi.",
            "Your IMOVA account has been deleted, together with its listings, photos, favorites and saved searches. " +
            "You won't get any more emails from us.",
            "Ваш аккаунт IMOVA удалён вместе с объявлениями, фотографиями, избранным и сохранёнными поисками. " +
            "Вы больше не будете получать от нас письма.");
        var messages = T(
            "Mesajele pe care le-ai trimis altor utilizatori rămân în conversațiile lor, fără numele sau datele tale de contact.",
            "Messages you sent to other users stay in their conversations, without your name or contact details.",
            "Сообщения, которые вы отправили другим пользователям, остаются в их переписке — без вашего имени и контактных данных.");
        var notYou = T(
            "Dacă nu tu ai șters contul, contactează echipa IMOVA cât mai curând.",
            "If you didn't delete the account, contact the IMOVA team as soon as possible.",
            "Если вы не удаляли аккаунт, как можно скорее свяжитесь с командой IMOVA.");

        var text = $"{greeting}\n\n{done}\n\n{messages}\n\n{notYou}\n";
        var html = EmailLayout.Page(
            title,
            EmailLayout.Heading(greeting)
            + EmailLayout.Paragraph(EmailLayout.Encode(done))
            + EmailLayout.Paragraph(EmailLayout.Encode(messages)),
            EmailLayout.Encode(notYou),
            language);

        return new EmailMessage(
            to, T("Contul tău IMOVA a fost șters", "Your IMOVA account has been deleted", "Ваш аккаунт IMOVA удалён"), text, html);
    }
}
