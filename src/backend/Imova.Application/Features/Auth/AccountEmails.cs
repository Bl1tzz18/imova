using Imova.Application.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Auth;

// The emails that carry an account link: confirm your address, reset your password. Both tokens
// come from Identity's default (data-protection) token provider, valid for LinkLifetime —
// Program.cs sets that as the provider's TokenLifespan.
public class AccountEmails(UserManager<ApplicationUser> userManager, IEmailSender emailSender, AppOptions appOptions)
{
    public static readonly TimeSpan LinkLifetime = TimeSpan.FromHours(24);

    public async Task SendEmailConfirmationAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = appOptions.WebUrl($"/confirm-email?userId={user.Id}&token={AccountTokens.Encode(token)}");
        var greeting = string.IsNullOrWhiteSpace(user.DisplayName) ? "Bună!" : $"Bună, {user.DisplayName}!";

        await emailSender.SendAsync(
            new EmailMessage(
                user.Email!,
                "Confirmă adresa de email — IMOVA",
                $"{greeting}\n\n" +
                "Confirmă adresa de email a contului tău IMOVA deschizând linkul de mai jos:\n\n" +
                $"{link}\n\n" +
                "După confirmare, anunțurile tale sunt trimise spre verificare și pot fi publicate. " +
                "Linkul este valabil 24 de ore.\n\n" +
                "Dacă nu tu ai creat acest cont, ignoră acest mesaj.\n"),
            cancellationToken);
    }

    public async Task SendPasswordResetAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var link = appOptions.WebUrl(
            $"/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={AccountTokens.Encode(token)}");

        await emailSender.SendAsync(
            new EmailMessage(
                user.Email!,
                "Resetează parola — IMOVA",
                "Am primit o cerere de resetare a parolei pentru contul tău IMOVA.\n\n" +
                "Alege o parolă nouă aici (linkul este valabil 24 de ore):\n\n" +
                $"{link}\n\n" +
                "Dacă nu tu ai cerut resetarea, ignoră acest mesaj — parola ta rămâne neschimbată.\n"),
            cancellationToken);
    }
}
