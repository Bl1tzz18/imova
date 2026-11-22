using System.Net.Http.Json;
using System.Text.Json;
using Imova.Application.Common.Interfaces;

namespace Imova.Infrastructure.Telegram;

// The Telegram Bot API over plain HTTP (https://core.telegram.org/bots/api) — only the handful of
// methods ITelegramBot needs, so no client library. The HttpClient's BaseAddress is
// https://api.telegram.org/bot<token>/ — the token is in every request's path, which is why the
// Worker registers this client with its request logging removed.
public sealed class TelegramBotClient(HttpClient httpClient) : ITelegramBot
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long? offset, int timeoutSeconds, CancellationToken cancellationToken)
    {
        var result = await CallAsync("getUpdates", new { offset, timeout = timeoutSeconds, allowedUpdates = new[] { "message" } }, cancellationToken);
        return result.EnumerateArray().Select(ReadUpdate).ToList();
    }

    public async Task SendMessageAsync(long chatId, string html, IReadOnlyList<TelegramButton> buttons, CancellationToken cancellationToken)
    {
        var payload = new
        {
            chatId,
            text = html,
            parseMode = "HTML",
            linkPreviewOptions = new { isDisabled = true },
            // One button per row: they read as a short list, and a long label isn't cut off.
            replyMarkup = buttons.Count == 0 ? null : new { inlineKeyboard = buttons.Select(b => new[] { new { text = b.Text, url = b.Url } }) },
        };

        try
        {
            await CallAsync("sendMessage", payload, cancellationToken);
        }
        catch (TelegramApiException ex) when (ex.ErrorCode == 403
                                              || (ex.ErrorCode == 400 && ex.Description.Contains("chat not found", StringComparison.OrdinalIgnoreCase)))
        {
            // 403: the user blocked the bot (or deleted their account); 400 "chat not found": the chat is gone.
            throw new TelegramChatUnavailableException(chatId, ex.Description);
        }
    }

    public async Task SetProfileAsync(string? languageCode, TelegramBotProfile profile, CancellationToken cancellationToken)
    {
        await CallAsync("setMyCommands", new
        {
            commands = profile.Commands.Select(c => new { command = c.Command, description = c.Description }),
            languageCode,
        }, cancellationToken);
        await CallAsync("setMyDescription", new { description = profile.Description, languageCode }, cancellationToken);
        await CallAsync("setMyShortDescription", new { shortDescription = profile.ShortDescription, languageCode }, cancellationToken);
    }

    public async Task DeleteWebhookAsync(CancellationToken cancellationToken) =>
        await CallAsync("deleteWebhook", new { dropPendingUpdates = false }, cancellationToken);

    // Every method answers {"ok": true, "result": …} or {"ok": false, "error_code": …, "description": …}.
    private async Task<JsonElement> CallAsync(string method, object payload, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(method, payload, JsonOptions, cancellationToken);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = document.RootElement;
        if (root.TryGetProperty("ok", out var ok) && ok.GetBoolean())
        {
            return root.GetProperty("result").Clone();
        }

        var code = root.TryGetProperty("error_code", out var errorCode) ? errorCode.GetInt32() : (int)response.StatusCode;
        var description = root.TryGetProperty("description", out var text) ? text.GetString() ?? "" : response.ReasonPhrase ?? "";
        throw new TelegramApiException(method, code, description);
    }

    private static TelegramUpdate ReadUpdate(JsonElement update)
    {
        var id = update.GetProperty("update_id").GetInt64();
        if (!update.TryGetProperty("message", out var message))
        {
            return new TelegramUpdate(id, null);
        }

        var chat = message.GetProperty("chat");
        var from = message.TryGetProperty("from", out var sender) ? sender : (JsonElement?)null;
        return new TelegramUpdate(id, new TelegramIncomingMessage(
            chat.GetProperty("id").GetInt64(),
            chat.TryGetProperty("type", out var type) && type.GetString() == "private",
            message.TryGetProperty("text", out var text) ? text.GetString() : null,
            String(from, "username"),
            String(from, "first_name"),
            String(from, "language_code")));
    }

    private static string? String(JsonElement? element, string property) =>
        element is { } e && e.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}

// A Bot API call Telegram refused. The message never contains the token.
public sealed class TelegramApiException(string method, int errorCode, string description)
    : Exception($"Telegram {method} failed: {errorCode} {description}")
{
    public int ErrorCode { get; } = errorCode;

    public string Description { get; } = description;
}
