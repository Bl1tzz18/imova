using System.Net;
using System.Net.Mail;
using Imova.Application.Common.Interfaces;

namespace Imova.Infrastructure.Email;

public class SmtpEmailSender(EmailOptions options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient(options.Host, options.Port) { EnableSsl = options.EnableSsl };
        if (!string.IsNullOrEmpty(options.Username))
        {
            client.Credentials = new NetworkCredential(options.Username, options.Password);
        }

        using var mail = new MailMessage(new MailAddress(options.FromAddress, options.FromName), new MailAddress(message.To))
        {
            Subject = message.Subject,
            Body = message.TextBody,
            IsBodyHtml = false,
        };
        await client.SendMailAsync(mail, cancellationToken);
    }
}
