using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
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
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
        };

        if (message.HtmlBody is not null)
        {
            mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.HtmlBody, Encoding.UTF8, MediaTypeNames.Text.Html));
        }

        await client.SendMailAsync(mail, cancellationToken);
    }
}
