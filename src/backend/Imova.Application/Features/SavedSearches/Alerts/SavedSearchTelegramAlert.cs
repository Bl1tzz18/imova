using Imova.Application.Common;
using Imova.Application.Common.Emails;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Telegram.Bot;
using Imova.Domain.SavedSearches;
using Microsoft.Extensions.Logging;
using static Imova.Application.Features.Telegram.Bot.TelegramBotTexts;

namespace Imova.Application.Features.SavedSearches.Alerts;

// A saved search's new listings as Telegram messages: one per listing (up to MaxListings) with its
// card photo, price, title, kind/size and place, a "Vezi anunțul" link and an "Oprește alerta"
// button (TelegramBotUpdates.HandleCallbackAsync); then, when there are more, one "and N more"
// message linking to all the results. Telegram fetches the photo itself when its URL is public;
// local storage isn't reachable from Telegram, so then the photo is uploaded with the message.
public class SavedSearchTelegramAlert(
    ITelegramBot bot,
    IBlobStorageService blobStorageService,
    AppOptions appOptions,
    ILogger<SavedSearchTelegramAlert> logger)
{
    public const int MaxListings = 5;

    // Telegram's caption limit is 1024 characters; a long title is shortened to stay well inside it.
    private const int MaxTitleLength = 200;

    // True when at least the first message went out — later failures (e.g. Telegram's rate limit)
    // only drop the rest, so a retry doesn't repeat what the user already got. Nothing sent → the
    // error is thrown and the job tries the same listings again next run. TelegramChatUnavailableException
    // (the user blocked the bot) always reaches the caller.
    public async Task<bool> SendAsync(
        long chatId, string language, SavedSearch savedSearch, IReadOnlyList<AlertListing> listings, int total,
        CancellationToken cancellationToken)
    {
        var shown = listings.Take(MaxListings).ToList();
        var sent = 0;
        foreach (var listing in shown)
        {
            var (caption, buttons) = TelegramHtml.WithButtons(
                Caption(language, savedSearch.Name, listing),
                new TelegramButton(ViewListingButton(language), listing.Url),
                new TelegramButton(StopAlertButton(language), CallbackData: TelegramBotUpdates.StopAlertData(savedSearch.Id)));
            try
            {
                await SendListingAsync(chatId, listing.PhotoUrl, caption, buttons, cancellationToken);
                sent++;
            }
            catch (Exception ex) when (ex is not TelegramChatUnavailableException and not OperationCanceledException && sent > 0)
            {
                logger.LogWarning(ex, "Stopped sending saved search {SavedSearchId}'s Telegram alert after {Sent} messages.", savedSearch.Id, sent);
                return true;
            }
        }

        if (sent > 0 && total > shown.Count)
        {
            var (html, buttons) = TelegramHtml.WithButtons(
                MoreListings(language, total - shown.Count, savedSearch.Name),
                new TelegramButton(AllResultsButton(language), appOptions.WebUrl($"/saved-searches/{savedSearch.Id}/open", language)));
            try
            {
                await bot.SendMessageAsync(chatId, html, buttons, cancellationToken);
            }
            catch (Exception ex) when (ex is not TelegramChatUnavailableException and not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not send the \"more listings\" message for saved search {SavedSearchId}.", savedSearch.Id);
            }
        }

        return sent > 0;
    }

    // <b>105 000 €</b> · Vânzare
    // Apartament 3 camere, bloc nou, Botanica
    //
    // 🏠 Apartament · 62 m²
    // 📍 Botanica, Chișinău
    //
    // 🔔 <i>Alertă pentru „Apartamente de vânzare”</i>
    public static string Caption(string language, string searchName, AlertListing listing)
    {
        var title = listing.Title.Length > MaxTitleLength ? listing.Title[..(MaxTitleLength - 1)] + "…" : listing.Title;
        var kind = string.Join(" · ", new[]
        {
            ListingEmailText.PropertyType(listing.PropertyType, language),
            ListingEmailText.Area(listing.AreaM2),
        }.Where(part => part is not null));

        var lines = new List<string>
        {
            $"<b>{TelegramHtml.Encode(Price(listing, language))}</b> · {TelegramHtml.Encode(ListingEmailText.Deal(listing.TransactionType, language))}",
            TelegramHtml.Encode(title),
            "",
            $"🏠 {TelegramHtml.Encode(kind)}",
        };
        if (listing.Location is not null)
        {
            lines.Add($"📍 {TelegramHtml.Encode(listing.Location)}");
        }

        lines.Add("");
        lines.Add($"🔔 <i>{AlertFor(language, searchName)}</i>");
        return string.Join("\n", lines);
    }

    // "105 000 €", "450 € / lună" — the currency as people write it (as the site's cards show it).
    private static string Price(AlertListing listing, string language)
    {
        var amount = listing.Amount.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');
        var currency = listing.Currency switch
        {
            "EUR" => "€",
            "USD" => "$",
            "MDL" => "lei",
            _ => listing.Currency,
        };
        return $"{amount} {currency}"
               + (listing.TransactionType == "Rent" ? Languages.Pick(language, " / lună", " / month", " / мес.") : string.Empty);
    }

    private async Task SendListingAsync(
        long chatId, string? photoUrl, string caption, IReadOnlyList<TelegramButton> buttons, CancellationToken cancellationToken)
    {
        var photo = await PhotoAsync(photoUrl, cancellationToken);
        if (photo is null)
        {
            await bot.SendMessageAsync(chatId, caption, buttons, cancellationToken);
            return;
        }

        try
        {
            await bot.SendPhotoAsync(chatId, photo, caption, buttons, cancellationToken);
        }
        catch (Exception ex) when (ex is not TelegramChatUnavailableException and not OperationCanceledException)
        {
            // A photo Telegram won't take shouldn't cost the user the listing.
            logger.LogWarning(ex, "Telegram refused a listing photo; sending the alert without it.");
            await bot.SendMessageAsync(chatId, caption, buttons, cancellationToken);
        }
    }

    private async Task<TelegramPhoto?> PhotoAsync(string? url, CancellationToken cancellationToken)
    {
        if (url is null)
        {
            return null;
        }

        if (TelegramHtml.CanLink(url))
        {
            return new TelegramPhoto(url);
        }

        if (blobStorageService.TryGetBlobNameFromUrl(url) is not { } blobName
            || await blobStorageService.OpenAsync(blobName, cancellationToken) is not { } stream)
        {
            return null;
        }

        await using (stream)
        {
            using var bytes = new MemoryStream();
            await stream.CopyToAsync(bytes, cancellationToken);
            return new TelegramPhoto(null, bytes.ToArray());
        }
    }
}
