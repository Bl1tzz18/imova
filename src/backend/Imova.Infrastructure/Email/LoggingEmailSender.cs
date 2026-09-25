using Imova.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Imova.Infrastructure.Email;

// Used when no SMTP host is configured: the email isn't sent, only logged.
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Email (not sent — no SMTP host configured) to {To}: {Subject}", message.To, message.Subject);
        return Task.CompletedTask;
    }
}
