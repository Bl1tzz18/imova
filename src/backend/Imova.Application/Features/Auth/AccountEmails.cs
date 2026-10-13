using Imova.Application.Common;
using Imova.Application.Common.Emails;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth;

// The emails that carry an account link: confirm your address, reset your password — HTML in the
// shared EmailLayout, plus the same text as plain text, in the user's language
// (ApplicationUser.PreferredLanguage). Both tokens come from Identity's default (data-protection)
// token provider, valid for LinkLifetime — Program.cs sets that as the provider's TokenLifespan.
public class AccountEmails(UserManager<ApplicationUser> userManager, IEmailSender emailSender, AppOptions appOptions)
{
    public static readonly TimeSpan LinkLifetime = TimeSpan.FromHours(24);

    public async Task SendEmailConfirmationAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = appOptions.WebUrl($"/confirm-email?userId={user.Id}&token={AccountTokens.Encode(token)}");
        await emailSender.SendAsync(
            ConfirmationEmail(user.Email!, user.DisplayName, link, user.PreferredLanguage), cancellationToken);
    }

    public async Task SendPasswordResetAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var link = appOptions.WebUrl(
            $"/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={AccountTokens.Encode(token)}");
        await emailSender.SendAsync(PasswordResetEmail(user.Email!, link, user.PreferredLanguage), cancellationToken);
    }

    // "Bună, Ana!" / "Hello, Ana!" / "Здравствуйте, Ana!" — plain when there's no name.
    public static string Greeting(string? displayName, string language) => string.IsNullOrWhiteSpace(displayName)
        ? Languages.Pick(language, "Bună!", "Hello!", "Здравствуйте!")
        : Languages.Pick(language, $"Bună, {displayName}!", $"Hello, {displayName}!", $"Здравствуйте, {displayName}!");

    public static EmailMessage ConfirmationEmail(string to, string? displayName, string link, string language)
    {
        string T(string ro, string en, string ru) => Languages.Pick(language, ro, en, ru);

        var greeting = Greeting(displayName, language);
        var intro = T(
            "Confirmă adresa de email a contului tău IMOVA.",
            "Please confirm the email address of your IMOVA account.",
            "Подтвердите адрес электронной почты вашего аккаунта IMOVA.");
        var openBelow = T("Deschide linkul de mai jos:", "Open the link below:", "Откройте ссылку ниже:");
        var after = T(
            "După confirmare, anunțurile tale sunt trimise spre verificare și pot fi publicate. Linkul este valabil 24 de ore.",
            "Once it's confirmed, your listings are sent for review and can be published. The link is valid for 24 hours.",
            "После подтверждения ваши объявления отправляются на проверку и могут быть опубликованы. Ссылка действительна 24 часа.");
        var ignore = T(
            "Dacă nu tu ai creat acest cont, ignoră acest mesaj.",
            "If you didn't create this account, you can ignore this email.",
            "Если вы не создавали этот аккаунт, просто проигнорируйте это письмо.");
        var title = T("Confirmă adresa de email", "Confirm your email address", "Подтвердите адрес электронной почты");

        var text = $"{greeting}\n\n{intro} {openBelow}\n\n{link}\n\n{after}\n\n{ignore}\n";
        var html = EmailLayout.Page(
            title,
            EmailLayout.Heading(greeting)
            + EmailLayout.Paragraph(EmailLayout.Encode(intro))
            + EmailLayout.Button(title, link)
            + EmailLayout.Paragraph(EmailLayout.Encode(after))
            + EmailLayout.LinkFallback(link, language),
            EmailLayout.Encode(ignore),
            language);

        return new EmailMessage(to, $"{title} — IMOVA", text, html);
    }

    public static EmailMessage PasswordResetEmail(string to, string link, string language)
    {
        string T(string ro, string en, string ru) => Languages.Pick(language, ro, en, ru);

        var title = T("Resetează parola", "Reset your password", "Сброс пароля");
        var intro = T(
            "Am primit o cerere de resetare a parolei pentru contul tău IMOVA.",
            "We received a request to reset the password of your IMOVA account.",
            "Мы получили запрос на сброс пароля вашего аккаунта IMOVA.");
        var choose = T(
            "Alege o parolă nouă folosind butonul de mai jos. Linkul este valabil 24 de ore și poate fi folosit o singură dată.",
            "Choose a new password with the button below. The link is valid for 24 hours and can be used only once.",
            "Выберите новый пароль с помощью кнопки ниже. Ссылка действительна 24 часа и может быть использована только один раз.");
        var chooseHere = T(
            "Alege o parolă nouă aici (linkul este valabil 24 de ore):",
            "Choose a new password here (the link is valid for 24 hours):",
            "Выберите новый пароль здесь (ссылка действительна 24 часа):");
        var ignore = T(
            "Dacă nu tu ai cerut resetarea, ignoră acest mesaj — parola ta rămâne neschimbată.",
            "If you didn't ask for this, ignore this email — your password stays the same.",
            "Если вы не запрашивали сброс, проигнорируйте это письмо — ваш пароль останется прежним.");
        var button = T("Alege o parolă nouă", "Choose a new password", "Выбрать новый пароль");

        var text = $"{intro}\n\n{chooseHere}\n\n{link}\n\n{ignore}\n";
        var html = EmailLayout.Page(
            title,
            EmailLayout.Heading(title)
            + EmailLayout.Paragraph(EmailLayout.Encode(intro))
            + EmailLayout.Paragraph(EmailLayout.Encode(choose))
            + EmailLayout.Button(button, link)
            + EmailLayout.LinkFallback(link, language),
            EmailLayout.Encode(ignore),
            language);

        return new EmailMessage(to, $"{title} — IMOVA", text, html);
    }
}
