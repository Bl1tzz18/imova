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

    // A photo with an HTML caption (≤1024 characters) and buttons; same errors as SendMessageAsync.
    Task SendPhotoAsync(long chatId, TelegramPhoto photo, string captionHtml, IReadOnlyList<TelegramButton> buttons, CancellationToken cancellationToken);

    // Acknowledges a button press (Telegram shows a spinner until then); `text` is a short toast.
    Task AnswerCallbackAsync(string callbackId, string? text, CancellationToken cancellationToken);

    // The bot's command menu and the texts shown before a chat starts, for one language (null =
    // the default for every language without its own).
    Task SetProfileAsync(string? languageCode, TelegramBotProfile profile, CancellationToken cancellationToken);

    // getUpdates refuses to run while a webhook is set.
    Task DeleteWebhookAsync(CancellationToken cancellationToken);
}

// One update from Telegram: a text message or a press on one of the bot's callback buttons (both
// null for anything else).
public sealed record TelegramUpdate(long UpdateId, TelegramIncomingMessage? Message, TelegramCallback? Callback = null);

// A press on a callback button under one of the bot's messages (TelegramButton.CallbackData).
public sealed record TelegramCallback(string Id, long ChatId, string? Data, string? LanguageCode);

public sealed record TelegramIncomingMessage(
    long ChatId,
    bool IsPrivateChat,
    string? Text,
    string? Username,
    string? FirstName,
    // The sender's Telegram app language ("ro", "ru", "en-GB", …), if Telegram sent it.
    string? LanguageCode);

// A button under a message: a link (Url) or a callback the bot receives when pressed (CallbackData,
// ≤64 bytes).
public sealed record TelegramButton(string Text, string? Url = null, string? CallbackData = null);

// A photo Telegram fetches itself (Url — must be publicly reachable) or one uploaded with the message (Bytes).
public sealed record TelegramPhoto(string? Url, byte[]? Bytes = null);

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
