using Imova.Application.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Imova.Application.Features.Telegram.Bot.TelegramBotTexts;

namespace Imova.Application.Features.Telegram.Bot;

// What the bot does with a message someone sent it (the Worker's TelegramUpdatePoller hands every
// one here, one at a time). Private chats only — in a group the bot stays silent.
//   /start <code>  the "Conectează Telegram" deep link: connects this chat to the code's account
//   /start         how to connect (or which account this chat is connected to)
//   /stop          disconnects the chat
//   /cautari       the account's saved searches, each linking to its results
//   anything else  the command list
// Every message gets exactly one reply.
public class TelegramBotUpdates(IApplicationDbContext dbContext, ITelegramBot bot, AppOptions appOptions, TimeProvider timeProvider)
{
    public async Task HandleAsync(TelegramIncomingMessage message, CancellationToken cancellationToken)
    {
        if (!message.IsPrivateChat || string.IsNullOrWhiteSpace(message.Text))
        {
            return;
        }

        var (command, argument) = ParseCommand(message.Text);
        var link = await dbContext.TelegramLinks.FirstOrDefaultAsync(l => l.ChatId == message.ChatId, cancellationToken);
        var user = link is null ? null : await dbContext.Users.FirstOrDefaultAsync(u => u.Id == link.UserId, cancellationToken);
        var language = LanguageFor(user?.PreferredLanguage, message.LanguageCode);

        var (html, buttons) = command switch
        {
            "/start" when argument is not null => await ConnectAsync(message, argument, language, cancellationToken),
            "/start" => user is null ? WithSettingsButton(Welcome(language), language) : (AlreadyConnected(language, user.Email!), []),
            "/stop" => await DisconnectAsync(link, language, cancellationToken),
            "/cautari" => user is null ? WithSettingsButton(Welcome(language), language) : await SavedSearchesAsync(user, language, cancellationToken),
            _ => user is null ? WithSettingsButton(Welcome(language), language) : (Help(language), []),
        };

        try
        {
            await bot.SendMessageAsync(message.ChatId, html, buttons, cancellationToken);
        }
        catch (TelegramChatUnavailableException)
        {
            // Blocked the bot between writing and our answer — nothing to send alerts to any more.
            await DropChatAsync(message.ChatId, cancellationToken);
        }
    }

    // "/start@ImovaMdBot abc" → ("/start", "abc"). Commands are matched case-insensitively.
    public static (string Command, string? Argument) ParseCommand(string text)
    {
        var trimmed = text.Trim();
        var space = trimmed.IndexOfAny([' ', '\n', '\t']);
        var command = (space < 0 ? trimmed : trimmed[..space]).ToLowerInvariant();
        var at = command.IndexOf('@');
        if (at > 0)
        {
            command = command[..at];
        }

        var argument = space < 0 ? null : trimmed[(space + 1)..].Trim();
        return (command, string.IsNullOrEmpty(argument) ? null : argument);
    }

    // Removes the chat's link — used when Telegram says the chat can't be written to any more.
    public async Task DropChatAsync(long chatId, CancellationToken cancellationToken)
    {
        var link = await dbContext.TelegramLinks.FirstOrDefaultAsync(l => l.ChatId == chatId, cancellationToken);
        if (link is not null)
        {
            dbContext.TelegramLinks.Remove(link);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<(string, IReadOnlyList<TelegramButton>)> ConnectAsync(
        TelegramIncomingMessage message, string code, string language, CancellationToken cancellationToken)
    {
        if (!TelegramLinkCodes.LooksValid(code))
        {
            return WithSettingsButton(LinkExpired(language), language);
        }

        var hash = TelegramLinkCodes.Hash(code);
        var linkCode = await dbContext.TelegramLinkCodes.FirstOrDefaultAsync(c => c.CodeHash == hash, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (linkCode is null || linkCode.ExpiresAt <= now)
        {
            if (linkCode is not null)
            {
                dbContext.TelegramLinkCodes.Remove(linkCode);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return WithSettingsButton(LinkExpired(language), language);
        }

        var owner = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == linkCode.UserId, cancellationToken);
        dbContext.TelegramLinkCodes.Remove(linkCode);
        if (owner is null)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return WithSettingsButton(LinkExpired(language), language);
        }

        // One account per chat: this chat stops getting another account's alerts…
        dbContext.TelegramLinks.RemoveRange(await dbContext.TelegramLinks
            .Where(l => l.ChatId == message.ChatId && l.UserId != owner.Id)
            .ToListAsync(cancellationToken));

        // …and one chat per account: connecting from a new chat moves the account here.
        var link = await dbContext.TelegramLinks.FirstOrDefaultAsync(l => l.UserId == owner.Id, cancellationToken);
        if (link is null)
        {
            link = new TelegramLink { UserId = owner.Id };
            dbContext.TelegramLinks.Add(link);
        }

        link.ChatId = message.ChatId;
        link.TelegramUsername = Truncate(message.Username, 64);
        link.TelegramFirstName = Truncate(message.FirstName, 128);
        link.LinkedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        return (Linked(Languages.Normalize(owner.PreferredLanguage), owner.Email!), []);
    }

    private async Task<(string, IReadOnlyList<TelegramButton>)> DisconnectAsync(
        TelegramLink? link, string language, CancellationToken cancellationToken)
    {
        if (link is null)
        {
            return WithSettingsButton(NotConnected(language), language);
        }

        dbContext.TelegramLinks.Remove(link);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (Disconnected(language), []);
    }

    private async Task<(string, IReadOnlyList<TelegramButton>)> SavedSearchesAsync(
        ApplicationUser user, string language, CancellationToken cancellationToken)
    {
        var searches = await dbContext.SavedSearches.AsNoTracking()
            .Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
        if (searches.Count == 0)
        {
            return TelegramHtml.WithButtons(NoSavedSearches(language), new TelegramButton(SearchButton(language), appOptions.WebUrl("/search")));
        }

        // The same "open" route as /saved-searches: marks the search viewed, then shows its results.
        var lines = searches.Select(s =>
            $"• {TelegramHtml.Link(s.Name, appOptions.WebUrl($"/saved-searches/{s.Id}/open"))} — {Frequency(language, s.AlertFrequency)}");
        return TelegramHtml.WithButtons(
            SavedSearchesHeading(language) + "\n\n" + string.Join("\n", lines),
            new TelegramButton(AllSavedSearchesButton(language), appOptions.WebUrl("/saved-searches")));
    }

    private (string, IReadOnlyList<TelegramButton>) WithSettingsButton(string html, string language) =>
        TelegramHtml.WithButtons(html, new TelegramButton(OpenSettingsButton(language), appOptions.WebUrl("/account?tab=notifications")));

    private static string? Truncate(string? value, int max) => value is null || value.Length <= max ? value : value[..max];
}
