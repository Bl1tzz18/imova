using System.Security.Cryptography;
using System.Text;

namespace Imova.Application.Features.Telegram;

// The codes in "Conectează Telegram" links. Telegram passes a deep link's start parameter on as
// "/start <code>" and allows at most 64 characters of A–Z, a–z, 0–9, "_" and "-" — so a random
// 32-byte value in Base64Url (43 characters), never a data-protection payload (far too long).
public static class TelegramLinkCodes
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    public const int Length = 43;

    public static string NewCode() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string code) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    // Rejects anything that can't be one of ours before it's hashed and looked up.
    public static bool LooksValid(string? code) =>
        code is { Length: Length } && code.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
