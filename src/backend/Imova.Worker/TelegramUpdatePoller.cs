using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Telegram.Bot;

namespace Imova.Worker;

// The bot's inbox: long-polls Telegram for messages people send it and hands each to
// TelegramBotUpdates, in order, each in its own DI scope. Long polling rather than a webhook: no
// public endpoint to secure, and it works the same on a laptop as in Azure — but only one process
// may poll a bot (Telegram answers 409 to a second one), hence a separate bot per environment and
// a single Worker instance.
//
// On start it also deletes any webhook (getUpdates refuses to run while one is set) and sets the
// bot's command menu and descriptions in ro (the default), en and ru.
public sealed class TelegramUpdatePoller(IServiceScopeFactory scopeFactory, ILogger<TelegramUpdatePoller> logger) : BackgroundService
{
    private const int PollSeconds = 50;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var profileSet = false;
        long? offset = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var bot = scope.ServiceProvider.GetRequiredService<ITelegramBot>();
                if (!profileSet)
                {
                    await SetUpAsync(bot, stoppingToken);
                    profileSet = true;
                }

                var updates = await bot.GetUpdatesAsync(offset, PollSeconds, stoppingToken);
                foreach (var update in updates)
                {
                    // Moved past before handling: a message that keeps failing is skipped, not retried forever.
                    offset = update.UpdateId + 1;
                    if (update.Message is not null || update.Callback is not null)
                    {
                        await HandleAsync(update, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Telegram unreachable, or 409: another process polls this bot (the same token on two machines?).
                logger.LogWarning(ex, "Telegram polling failed; retrying in {Delay}.", RetryDelay);
                await Task.Delay(RetryDelay, stoppingToken);
            }
        }
    }

    private async Task HandleAsync(TelegramUpdate update, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var updates = scope.ServiceProvider.GetRequiredService<TelegramBotUpdates>();
            if (update.Message is not null)
            {
                await updates.HandleAsync(update.Message, stoppingToken);
            }
            else
            {
                await updates.HandleCallbackAsync(update.Callback!, stoppingToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Only the chat id — never the message text, which may hold a link code.
            logger.LogError(ex, "Handling a Telegram update from chat {ChatId} failed.", update.Message?.ChatId ?? update.Callback?.ChatId);
        }
    }

    private static async Task SetUpAsync(ITelegramBot bot, CancellationToken cancellationToken)
    {
        await bot.DeleteWebhookAsync(cancellationToken);
        await bot.SetProfileAsync(null, TelegramBotTexts.Profile(Languages.Romanian), cancellationToken);
        await bot.SetProfileAsync(Languages.English, TelegramBotTexts.Profile(Languages.English), cancellationToken);
        await bot.SetProfileAsync(Languages.Russian, TelegramBotTexts.Profile(Languages.Russian), cancellationToken);
    }
}
