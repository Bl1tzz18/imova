using Imova.Application.Common.Interfaces;

namespace Imova.UnitTests.TestSupport;

// Records what the bot would send; UnavailableChats makes SendMessageAsync throw like Telegram does
// for a chat that blocked the bot.
internal sealed class FakeTelegramBot : ITelegramBot
{
    public List<(long ChatId, string Html, IReadOnlyList<TelegramButton> Buttons)> Sent { get; } = [];

    public HashSet<long> UnavailableChats { get; } = [];

    public List<(string? LanguageCode, TelegramBotProfile Profile)> Profiles { get; } = [];

    public List<(long ChatId, TelegramPhoto Photo, string Caption, IReadOnlyList<TelegramButton> Buttons)> Photos { get; } = [];

    public List<(string CallbackId, string? Text)> Answers { get; } = [];

    // Every send after this many messages and photos fails like Telegram's rate limit would (null: never).
    public int? FailAfter { get; set; }

    // SendPhotoAsync fails (as for a photo Telegram won't take); SendMessageAsync still works.
    public bool RefusePhotos { get; set; }

    public Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long? offset, int timeoutSeconds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TelegramUpdate>>([]);

    public Task SendMessageAsync(long chatId, string html, IReadOnlyList<TelegramButton> buttons, CancellationToken cancellationToken)
    {
        Check(chatId);
        Sent.Add((chatId, html, buttons));
        return Task.CompletedTask;
    }

    public Task SendPhotoAsync(
        long chatId, TelegramPhoto photo, string captionHtml, IReadOnlyList<TelegramButton> buttons, CancellationToken cancellationToken)
    {
        Check(chatId);
        if (RefusePhotos)
        {
            throw new InvalidOperationException("Telegram sendPhoto failed: 400 Bad Request: wrong file");
        }

        Photos.Add((chatId, photo, captionHtml, buttons));
        return Task.CompletedTask;
    }

    public Task AnswerCallbackAsync(string callbackId, string? text, CancellationToken cancellationToken)
    {
        Answers.Add((callbackId, text));
        return Task.CompletedTask;
    }

    private void Check(long chatId)
    {
        if (UnavailableChats.Contains(chatId))
        {
            throw new TelegramChatUnavailableException(chatId, "Forbidden: bot was blocked by the user");
        }

        if (FailAfter is { } limit && Sent.Count + Photos.Count >= limit)
        {
            throw new InvalidOperationException("Telegram sendMessage failed: 429 Too Many Requests");
        }
    }

    public Task SetProfileAsync(string? languageCode, TelegramBotProfile profile, CancellationToken cancellationToken)
    {
        Profiles.Add((languageCode, profile));
        return Task.CompletedTask;
    }

    public Task DeleteWebhookAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
