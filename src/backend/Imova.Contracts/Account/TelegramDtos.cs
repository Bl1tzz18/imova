namespace Imova.Contracts.Account;

// The Telegram block of the account's "Notificări" tab. Available: the site has a bot configured
// (otherwise the block is hidden). Username/FirstName come from Telegram and either may be null.
public record TelegramConnectionDto(
    bool Available,
    bool Connected,
    string? Username,
    string? FirstName,
    DateTimeOffset? ConnectedAt);

// "Conectează Telegram": the t.me deep link to open (it carries a one-time code) and when it stops working.
public record TelegramLinkStartDto(string Url, DateTimeOffset ExpiresAt);
