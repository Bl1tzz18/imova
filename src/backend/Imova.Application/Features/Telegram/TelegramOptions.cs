namespace Imova.Application.Features.Telegram;

// Bound from the "Telegram" configuration section. Without a BotUsername the API says Telegram
// isn't available (the account page hides the button); without a BotToken the Worker doesn't
// start the bot. Each environment has its own bot: only one process may poll a bot's updates.
public class TelegramOptions
{
    public const string SectionName = "Telegram";

    // Secret — .env / user secrets locally, Key Vault in Azure. Only the Worker needs it.
    public string? BotToken { get; set; }

    // With or without the "@", e.g. "ImovaMdBot".
    public string? BotUsername { get; set; }

    public bool IsAvailable => !string.IsNullOrWhiteSpace(BotUsername);

    public string StartUrl(string code) => $"https://t.me/{BotUsername!.Trim().TrimStart('@')}?start={code}";
}
