using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Imova.Application.Common.Interfaces;

namespace Imova.IntegrationTests.TestSupport;

// Replaces the real IEmailSender so a test can read what "was sent" — the account links in particular.
internal sealed partial class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<EmailMessage> SentTo(string address) =>
        _sent.Where(m => string.Equals(m.To, address, StringComparison.OrdinalIgnoreCase)).ToList();

    // The query parameters of the first link in the latest email to `address`.
    public IReadOnlyDictionary<string, string> LinkQuery(string address)
    {
        var body = SentTo(address).Last().TextBody;
        var link = new Uri(LinkPattern().Match(body).Value);
        return link.Query.TrimStart('?').Split('&')
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(kv => kv[0], kv => Uri.UnescapeDataString(kv[1]));
    }

    [GeneratedRegex(@"https?://\S+")]
    private static partial Regex LinkPattern();
}
