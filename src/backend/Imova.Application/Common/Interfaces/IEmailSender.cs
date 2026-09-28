namespace Imova.Application.Common.Interfaces;

// TextBody is always sent; HtmlBody, when given, goes alongside it (multipart/alternative) —
// email apps show the HTML version and fall back to the text one.
public record EmailMessage(string To, string Subject, string TextBody, string? HtmlBody = null);

// Outgoing email (Imova.Infrastructure/Email): SMTP when configured, otherwise only logged.
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
