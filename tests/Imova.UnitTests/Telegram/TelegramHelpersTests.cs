using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Telegram;
using Imova.Application.Features.Telegram.Bot;

namespace Imova.UnitTests.Telegram;

public class TelegramHelpersTests
{
    [Fact]
    public void NewCode_FitsTelegramsStartParameter()
    {
        var codes = Enumerable.Range(0, 200).Select(_ => TelegramLinkCodes.NewCode()).ToList();

        // ≤64 characters of A–Z a–z 0–9 _ -
        Assert.All(codes, code => Assert.True(TelegramLinkCodes.LooksValid(code), code));
        Assert.All(codes, code => Assert.Equal(43, code.Length));
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa+")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void LooksValid_RejectsAnythingThatIsNotOneOfOurCodes(string? code) =>
        Assert.False(TelegramLinkCodes.LooksValid(code));

    [Fact]
    public void Hash_IsStableHexSha256()
    {
        var hash = TelegramLinkCodes.Hash("abc");

        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", hash);
    }

    [Fact]
    public void StartUrl_IsTheBotsDeepLink_WithOrWithoutTheAt()
    {
        Assert.Equal("https://t.me/ImovaDevBot?start=x", new TelegramOptions { BotUsername = "@ImovaDevBot " }.StartUrl("x"));
        Assert.Equal("https://t.me/ImovaDevBot?start=x", new TelegramOptions { BotUsername = "ImovaDevBot" }.StartUrl("x"));
        Assert.False(new TelegramOptions { BotUsername = " " }.IsAvailable);
    }

    [Fact]
    public void Encode_EscapesTelegramsHtmlSpecials() =>
        Assert.Equal("Casa &lt;b&gt; &amp; curte", TelegramHtml.Encode("Casa <b> & curte"));

    [Theory]
    [InlineData("https://imova.md/search", true)]
    [InlineData("http://imova.md/search", true)]
    [InlineData("http://localhost:3000/search", false)]
    [InlineData("http://127.0.0.1:3000/search", false)]
    [InlineData("http://0.0.0.0:3000/search", false)]
    [InlineData("http://192.168.1.10/search", false)]
    [InlineData("http://10.0.0.5/search", false)]
    [InlineData("http://172.20.0.5/search", false)]
    [InlineData("ftp://imova.md/file", false)]
    [InlineData("/search", false)]
    public void CanLink_OnlyAddressesTelegramAccepts(string url, bool expected) =>
        Assert.Equal(expected, TelegramHtml.CanLink(url));

    [Fact]
    public void Link_IsAnAnchorForAPublicAddress_PlainTextForALocalOne()
    {
        Assert.Equal("<a href=\"https://imova.md/s?a=1&amp;b=2\">Botanica &lt;2&gt;</a>", TelegramHtml.Link("Botanica <2>", "https://imova.md/s?a=1&b=2"));
        Assert.Equal("Botanica", TelegramHtml.Link("Botanica", "http://localhost:3000/s"));
    }

    [Fact]
    public void WithButtons_LeavesOutTheLinksTelegramWouldRefuse_KeepsCallbacks()
    {
        var (html, buttons) = TelegramHtml.WithButtons("Salut",
            new TelegramButton("Public", "https://imova.md/a"),
            new TelegramButton("Local", "http://localhost:3000/b"),
            new TelegramButton("Stop", CallbackData: "ss-off:x"));

        Assert.Equal(["Public", "Stop"], buttons.Select(b => b.Text));
        Assert.Equal("Salut", html);
    }

    [Theory]
    [InlineData(null, "ru-RU", "ru")]
    [InlineData(null, "en", "en")]
    [InlineData(null, "uk", "ro")]
    [InlineData(null, null, "ro")]
    [InlineData("en", "ru", "en")]
    public void LanguageFor_PrefersTheAccountThenTheTelegramApp(string? account, string? telegram, string expected) =>
        Assert.Equal(expected, TelegramBotTexts.LanguageFor(account, telegram));

    [Theory]
    [InlineData("ro")]
    [InlineData("en")]
    [InlineData("ru")]
    public void Profile_FitsTelegramsLimits(string language)
    {
        var profile = TelegramBotTexts.Profile(language);

        Assert.True(profile.Description.Length <= 512);
        Assert.True(profile.ShortDescription.Length <= 120);
        Assert.All(profile.Commands, c => Assert.Matches("^[a-z0-9_]{1,32}$", c.Command));
        Assert.All(profile.Commands, c => Assert.InRange(c.Description.Length, 1, 256));
    }

    [Theory]
    [InlineData("/start abc", "/start", "abc")]
    [InlineData("  /START   abc  ", "/start", "abc")]
    [InlineData("/start@ImovaDevBot abc", "/start", "abc")]
    [InlineData("/stop", "/stop", null)]
    [InlineData("/cautari@ImovaDevBot", "/cautari", null)]
    [InlineData("salut", "salut", null)]
    public void ParseCommand_SplitsCommandAndArgument(string text, string command, string? argument) =>
        Assert.Equal((command, argument), TelegramBotUpdates.ParseCommand(text));
}
