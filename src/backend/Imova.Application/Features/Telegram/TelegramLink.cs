namespace Imova.Application.Features.Telegram;

// An IMOVA account's Telegram chat with the bot — where its Telegram alerts go. At most one per
// account and one account per chat (connecting a chat that belonged to another account moves it).
// Made when the user presses Start on a link-code deep link (TelegramBotUpdates); removed by /stop,
// "Deconectează" in the account settings, a chat that blocked the bot, or deleting the account.
public sealed class TelegramLink
{
    public Guid UserId { get; set; }

    // The private chat's id (= the Telegram user's id). Personal data: exported, deleted with the account.
    public long ChatId { get; set; }

    // Only to show "@name" in the account settings; may be absent on Telegram.
    public string? TelegramUsername { get; set; }

    public string? TelegramFirstName { get; set; }

    public DateTimeOffset LinkedAt { get; set; }
}

// A one-time code in the "Conectează Telegram" link (t.me/<bot>?start=<code>). Only its SHA-256
// hash is stored, like refresh tokens; it's deleted when used and dies after
// TelegramLinkCodes.Lifetime. A new link replaces the account's earlier unused codes.
public sealed class TelegramLinkCode
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string CodeHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
