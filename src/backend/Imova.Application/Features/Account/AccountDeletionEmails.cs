using Imova.Application.Common;
using Imova.Application.Common.Emails;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Auth;
using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Features.Account;

// The two emails around deleting an account, in the shared EmailLayout (HTML + the same text as
// plain text), Romanian like the other account emails:
// - the confirmation link, for an account without a password (signed up with Google) — opening it
//   leads to a page with a final "delete" button, never deletes by itself (mail scanners open links);
// - the notice once the account is gone, so an unexpected deletion doesn't go unnoticed.
public class AccountDeletionEmails(UserManager<ApplicationUser> userManager, IEmailSender emailSender, AppOptions appOptions)
{
    public async Task SendConfirmationLinkAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, AccountDeletion.TokenPurpose);
        var link = appOptions.WebUrl($"/delete-account?userId={user.Id}&token={AccountTokens.Encode(token)}");
        await emailSender.SendAsync(ConfirmationEmail(user.Email!, user.DisplayName, link), cancellationToken);
    }

    public Task SendDeletedAsync(string email, string? displayName, CancellationToken cancellationToken) =>
        emailSender.SendAsync(DeletedEmail(email, displayName), cancellationToken);

    public static EmailMessage ConfirmationEmail(string to, string? displayName, string link)
    {
        var greeting = Greeting(displayName);
        const string intro = "Am primit o cerere de ștergere a contului tău IMOVA.";
        const string what =
            "Ștergerea este definitivă: contul, anunțurile tale (cu fotografiile lor), favoritele și căutările salvate " +
            "vor fi șterse și nu mai pot fi recuperate. Deschide linkul de mai jos și confirmă pe pagină. " +
            "Linkul este valabil 24 de ore.";
        const string ignore = "Dacă nu tu ai cerut ștergerea, ignoră acest mesaj — contul tău rămâne neschimbat.";

        var text = $"{greeting}\n\n{intro}\n\n{what}\n\n{link}\n\n{ignore}\n";
        var html = EmailLayout.Page(
            "Confirmă ștergerea contului",
            EmailLayout.Heading(greeting)
            + EmailLayout.Paragraph(EmailLayout.Encode(intro))
            + EmailLayout.Paragraph(EmailLayout.Encode(what))
            + EmailLayout.Button("Continuă spre ștergerea contului", link)
            + EmailLayout.LinkFallback(link),
            EmailLayout.Encode(ignore));

        return new EmailMessage(to, "Confirmă ștergerea contului — IMOVA", text, html);
    }

    public static EmailMessage DeletedEmail(string to, string? displayName)
    {
        var greeting = Greeting(displayName);
        const string done =
            "Contul tău IMOVA a fost șters, împreună cu anunțurile, fotografiile, favoritele și căutările salvate. " +
            "Nu vei mai primi emailuri de la noi.";
        const string messages =
            "Mesajele pe care le-ai trimis altor utilizatori rămân în conversațiile lor, fără numele sau datele tale de contact.";
        const string notYou = "Dacă nu tu ai șters contul, contactează echipa IMOVA cât mai curând.";

        var text = $"{greeting}\n\n{done}\n\n{messages}\n\n{notYou}\n";
        var html = EmailLayout.Page(
            "Contul tău a fost șters",
            EmailLayout.Heading(greeting)
            + EmailLayout.Paragraph(EmailLayout.Encode(done))
            + EmailLayout.Paragraph(EmailLayout.Encode(messages)),
            EmailLayout.Encode(notYou));

        return new EmailMessage(to, "Contul tău IMOVA a fost șters", text, html);
    }

    private static string Greeting(string? displayName) =>
        string.IsNullOrWhiteSpace(displayName) ? "Bună!" : $"Bună, {displayName}!";
}
