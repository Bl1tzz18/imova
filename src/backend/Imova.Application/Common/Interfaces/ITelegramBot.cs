namespace Imova.Application.Common.Interfaces;

// The IMOVA Telegram bot as the Application sees it — the few Bot API methods it uses
// (Infrastructure's TelegramBotClient). Only the Worker talks to Telegram; the API never needs the
// bot's token.
public interface ITelegramBot
{
    // Long polling: waits up to `timeoutSeconds` for updates after `offset` (the next update id —
    // asking for it also tells Telegram every earlier one was handled).
    Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long? offset, int timeoutSeconds, CancellationToken cancellationToken);

    // `html` uses Telegram's HTML subset (TelegramHtml). Throws TelegramChatUnavailableException
    // when the chat can't be written to any more (the user blocked the bot or deleted the chat).
    Task SendMessageAsync(long chatId, string html, IReadOnlyList<TelegramButton> buttons, CancellationToken cancellationToken);

    // The bot's command menu and the texts shown before a chat starts, for one language (null =
    // the default for every language without its own).
    Task SetProfileAsync(string? languageCode, TelegramBotProfile profile, CancellationToken cancellationToken);

    // getUpdates refuses to run while a webhook is set.
    Task DeleteWebhookAsync(CancellationToken cancellationToken);
}

// One update from Telegram; only text messages are kept (Message is null otherwise).
public sealed record TelegramUpdate(long UpdateId, TelegramIncomingMessage? Message);

public sealed record TelegramIncomingMessage(
    long ChatId,
    bool IsPrivateChat,
    string? Text,
    string? Username,
    string? FirstName,
    // The sender's Telegram app language ("ro", "ru", "en-GB", …), if Telegram sent it.
    string? LanguageCode);

// A link button under a message.
public sealed record TelegramButton(string Text, string Url);

public sealed record TelegramBotCommand(string Command, string Description);

public sealed record TelegramBotProfile(
    IReadOnlyList<TelegramBotCommand> Commands,
    // Shown in an empty chat, before Start (≤512 characters).
    string Description,
    // The bot's profile page and share previews (≤120 characters).
    string ShortDescription);

public sealed class TelegramChatUnavailableException(long chatId, string reason)
    : Exception($"Telegram chat {chatId} can't be written to: {reason}")
{
    public long ChatId { get; } = chatId;
}
