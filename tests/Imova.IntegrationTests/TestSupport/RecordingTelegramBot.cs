using Imova.Application.Common.Interfaces;

namespace Imova.IntegrationTests.TestSupport;

// Telegram for integration tests: records what the bot would send, never calls Telegram.
public sealed class RecordingTelegramBot : ITelegramBot
{
    public List<(long ChatId, string Html, IReadOnlyList<TelegramButton> Buttons)> Sent { get; } = [];

    public List<(long ChatId, TelegramPhoto Photo, string Caption, IReadOnlyList<TelegramButton> Buttons)> Photos { get; } = [];

    public Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long? offset, int timeoutSeconds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TelegramUpdate>>([]);

    public Task SendMessageAsync(long chatId, string html, IReadOnlyList<TelegramButton> buttons, CancellationToken cancellationToken)
    {
        Sent.Add((chatId, html, buttons));
        return Task.CompletedTask;
    }

    public Task SendPhotoAsync(
        long chatId, TelegramPhoto photo, string captionHtml, IReadOnlyList<TelegramButton> buttons, CancellationToken cancellationToken)
    {
        Photos.Add((chatId, photo, captionHtml, buttons));
        return Task.CompletedTask;
    }

    public Task AnswerCallbackAsync(string callbackId, string? text, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task SetProfileAsync(string? languageCode, TelegramBotProfile profile, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task DeleteWebhookAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
