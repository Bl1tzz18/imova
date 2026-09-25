namespace Imova.Application.Common.Interfaces;

public record EmailMessage(string To, string Subject, string TextBody);

// Outgoing email (Imova.Infrastructure/Email): SMTP when configured, otherwise only logged.
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
