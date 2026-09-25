namespace Imova.Infrastructure.Email;

// Bound from the "Email" configuration section. Without a Host, emails are only logged (see
// LoggingEmailSender) — docker compose points it at Mailpit (http://localhost:8025) for local dev.
public class EmailOptions
{
    public const string SectionName = "Email";

    public string? Host { get; set; }

    public int Port { get; set; } = 25;

    public bool EnableSsl { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = "no-reply@imova.md";

    public string FromName { get; set; } = "IMOVA";
}
