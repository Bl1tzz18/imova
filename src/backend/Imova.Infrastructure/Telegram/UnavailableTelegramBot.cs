using Imova.Application.Common.Interfaces;

namespace Imova.Infrastructure.Telegram;

// The ITelegramBot of a process without a bot token: the API (it never talks to Telegram — this
// only lets the Worker's jobs be built from its container, as the integration tests do) and a
// Worker with no "Telegram:BotToken". The alert job doesn't call it then (TelegramOptions has no
// token), so any call is a bug.
public sealed class UnavailableTelegramBot : ITelegramBot
{
    private static InvalidOperationException NoBot() => new("No Telegram bot is configured in this process.");

    public Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long? offset, int timeoutSeconds, CancellationToken cancellationToken) => throw NoBot();

    public Task SendMessageAsync(long chatId, string html, IReadOnlyList<TelegramButton> buttons, CancellationToken cancellationToken) => throw NoBot();

    public Task SendPhotoAsync(
        long chatId, TelegramPhoto photo, string captionHtml, IReadOnlyList<TelegramButton> buttons, CancellationToken cancellationToken) =>
        throw NoBot();

    public Task AnswerCallbackAsync(string callbackId, string? text, CancellationToken cancellationToken) => throw NoBot();

    public Task SetProfileAsync(string? languageCode, TelegramBotProfile profile, CancellationToken cancellationToken) => throw NoBot();

    public Task DeleteWebhookAsync(CancellationToken cancellationToken) => throw NoBot();
}
