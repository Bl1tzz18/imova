namespace Imova.Application.Features.Messaging;

// Bound from the "Messaging" configuration section.
public class MessagingOptions
{
    public const string SectionName = "Messaging";

    // Anti-spam: how many *new* conversations one user may start per rolling hour (reusing an
    // existing one doesn't count).
    public int MaxNewConversationsPerHour { get; set; } = 10;
}
