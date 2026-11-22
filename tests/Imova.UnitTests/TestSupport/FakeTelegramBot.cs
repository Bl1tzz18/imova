using Imova.Application.Common.Interfaces;

namespace Imova.UnitTests.TestSupport;

// Records what the bot would send; UnavailableChats makes SendMessageAsync throw like Telegram does
// for a chat that blocked the bot.
internal sealed class FakeTelegramBot : ITelegramBot
{
    public List<(long ChatId, string Html, IReadOnlyList<TelegramButton> Buttons)> Sent { get; } = [];

    public HashSet<long> UnavailableChats { get; } = [];

    public List<(string? LanguageCode, TelegramBotProfile Profile)> Profiles { get; } = [];

    public Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long? offset, int timeoutSeconds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TelegramUpdate>>([]);

    public Task SendMessageAsync(long chatId, string html, IReadOnlyList<TelegramButton> buttons, CancellationToken cancellationToken)
    {
        if (UnavailableChats.Contains(chatId))
        {
            throw new TelegramChatUnavailableException(chatId, "Forbidden: bot was blocked by the user");
        }

        Sent.Add((chatId, html, buttons));
        return Task.CompletedTask;
    }

    public Task SetProfileAsync(string? languageCode, TelegramBotProfile profile, CancellationToken cancellationToken)
    {
        Profiles.Add((languageCode, profile));
        return Task.CompletedTask;
    }

    public Task DeleteWebhookAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
