using Imova.Application.Common;

namespace Imova.UnitTests.Common;

public class AppOptionsTests
{
    private readonly AppOptions _options = new() { WebBaseUrl = "https://imova.md/" };

    [Fact]
    public void WebUrl_JoinsTheSiteAndThePath()
    {
        Assert.Equal("https://imova.md/search?a=1", _options.WebUrl("/search?a=1"));
    }

    [Theory]
    [InlineData("ro", "https://imova.md/messages/1")]
    [InlineData("ru", "https://imova.md/ru/messages/1")]
    [InlineData("en", "https://imova.md/en/messages/1")]
    // An unknown or missing language is Romanian, like the emails themselves.
    [InlineData("de", "https://imova.md/messages/1")]
    [InlineData(null, "https://imova.md/messages/1")]
    public void WebUrl_InALanguage_UsesTheWebAppsAddressForIt(string? language, string expected)
    {
        Assert.Equal(expected, _options.WebUrl("/messages/1", language));
    }
}
