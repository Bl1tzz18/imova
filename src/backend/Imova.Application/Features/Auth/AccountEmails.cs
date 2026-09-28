using Imova.Application.Common;
using Imova.Application.Common.Emails;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth;

// The emails that carry an account link: confirm your address, reset your password — HTML in the
// shared EmailLayout, plus the same text as plain text. Both tokens come from Identity's default
// (data-protection) token provider, valid for LinkLifetime — Program.cs sets that as the
// provider's TokenLifespan.
public class AccountEmails(UserManager<ApplicationUser> userManager, IEmailSender emailSender, AppOptions appOptions)
{
    public static readonly TimeSpan LinkLifetime = TimeSpan.FromHours(24);

    public async Task SendEmailConfirmationAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = appOptions.WebUrl($"/confirm-email?userId={user.Id}&token={AccountTokens.Encode(token)}");
        await emailSender.SendAsync(ConfirmationEmail(user.Email!, user.DisplayName, link), cancellationToken);
    }

    public async Task SendPasswordResetAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var link = appOptions.WebUrl(
            $"/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={AccountTokens.Encode(token)}");
        await emailSender.SendAsync(PasswordResetEmail(user.Email!, link), cancellationToken);
    }

    public static EmailMessage ConfirmationEmail(string to, string? displayName, string link)
    {
        var greeting = string.IsNullOrWhiteSpace(displayName) ? "Bună!" : $"Bună, {displayName}!";
        const string intro = "Confirmă adresa de email a contului tău IMOVA.";
        const string after = "După confirmare, anunțurile tale sunt trimise spre verificare și pot fi publicate. Linkul este valabil 24 de ore.";
        const string ignore = "Dacă nu tu ai creat acest cont, ignoră acest mesaj.";

        var text = $"{greeting}\n\n{intro} Deschide linkul de mai jos:\n\n{link}\n\n{after}\n\n{ignore}\n";
        var html = EmailLayout.Page(
            "Confirmă adresa de email",
            EmailLayout.Heading(greeting)
            + EmailLayout.Paragraph(EmailLayout.Encode(intro))
            + EmailLayout.Button("Confirmă adresa de email", link)
            + EmailLayout.Paragraph(EmailLayout.Encode(after))
            + EmailLayout.LinkFallback(link),
            EmailLayout.Encode(ignore));

        return new EmailMessage(to, "Confirmă adresa de email — IMOVA", text, html);
    }

    public static EmailMessage PasswordResetEmail(string to, string link)
    {
        const string intro = "Am primit o cerere de resetare a parolei pentru contul tău IMOVA.";
        const string choose = "Alege o parolă nouă folosind butonul de mai jos. Linkul este valabil 24 de ore și poate fi folosit o singură dată.";
        const string ignore = "Dacă nu tu ai cerut resetarea, ignoră acest mesaj — parola ta rămâne neschimbată.";

        var text = $"{intro}\n\nAlege o parolă nouă aici (linkul este valabil 24 de ore):\n\n{link}\n\n{ignore}\n";
        var html = EmailLayout.Page(
            "Resetează parola",
            EmailLayout.Heading("Resetează parola")
            + EmailLayout.Paragraph(EmailLayout.Encode(intro))
            + EmailLayout.Paragraph(EmailLayout.Encode(choose))
            + EmailLayout.Button("Alege o parolă nouă", link)
            + EmailLayout.LinkFallback(link),
            EmailLayout.Encode(ignore));

        return new EmailMessage(to, "Resetează parola — IMOVA", text, html);
    }
}
