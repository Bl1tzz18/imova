using System.Diagnostics;
using Imova.Infrastructure.Monitoring;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Imova.UnitTests.Monitoring;

public class TelemetryRedactionTests
{
    [Theory]
    // The Telegram bot's token is in the path of every Bot API call.
    [InlineData("https://api.telegram.org/bot123456:AAH-secret_token/getUpdates", "https://api.telegram.org/botREDACTED/getUpdates")]
    [InlineData("https://api.telegram.org/bot123456:AAH-secret/sendPhoto?chat_id=42", "https://api.telegram.org/botREDACTED/sendPhoto?chat_id=REDACTED")]
    // Query values (SAS signatures, searched addresses, our contact email for Nominatim) go; names stay.
    [InlineData("https://x.blob.core.windows.net/c/a.jpg?sv=2024&sig=abc%2B", "https://x.blob.core.windows.net/c/a.jpg?sv=REDACTED&sig=REDACTED")]
    [InlineData("https://nominatim.openstreetmap.org/search?q=Str.+Mihai+Eminescu+5&email=a%40b.md&format=json",
        "https://nominatim.openstreetmap.org/search?q=REDACTED&email=REDACTED&format=REDACTED")]
    [InlineData("/reset-password?email=a@b.md&token=xyz#top", "/reset-password?email=REDACTED&token=REDACTED#top")]
    [InlineData("/search?flag", "/search?flag=REDACTED")]
    // Nothing secret: unchanged.
    [InlineData("https://imova.md/api/v1/listings/123", "https://imova.md/api/v1/listings/123")]
    [InlineData("/robots.txt?", "/robots.txt?")]
    public void Url_DropsTokensAndQueryValues(string url, string expected)
    {
        Assert.Equal(expected, TelemetryRedaction.Url(url));
    }

    [Fact]
    public void Tag_UrlQueryHoldsOnlyTheQuery()
    {
        Assert.Equal("token=REDACTED&id=REDACTED", TelemetryRedaction.Tag("url.query", "token=abc&id=1"));
        Assert.Equal("?token=REDACTED", TelemetryRedaction.Tag("url.query", "?token=abc"));
    }

    [Fact]
    public void Processor_RedactsEveryUrlTagAndTheName()
    {
        using var activity = new Activity("GET https://api.telegram.org/botsecret/getUpdates");
        activity.SetTag("url.full", "https://api.telegram.org/botsecret/getUpdates?offset=5");
        activity.SetTag("url.query", "offset=5");
        activity.SetTag("http.url", "https://api.telegram.org/botsecret/getMe");
        activity.SetTag("server.address", "api.telegram.org");
        activity.DisplayName = "GET https://api.telegram.org/botsecret/getUpdates";

        new TelemetryRedactionProcessor().OnEnd(activity);

        Assert.Equal("https://api.telegram.org/botREDACTED/getUpdates?offset=REDACTED", activity.GetTagItem("url.full"));
        Assert.Equal("offset=REDACTED", activity.GetTagItem("url.query"));
        Assert.Equal("https://api.telegram.org/botREDACTED/getMe", activity.GetTagItem("http.url"));
        Assert.Equal("api.telegram.org", activity.GetTagItem("server.address"));
        Assert.DoesNotContain("secret", activity.DisplayName);
    }

    [Theory]
    [InlineData("/health", false)]
    [InlineData("/health/ready", false)]
    [InlineData("/api/v1/listings", true)]
    [InlineData("/healthy-homes", true)]
    public void HealthProbes_AreNotRecorded(string path, bool recorded)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        Assert.Equal(recorded, Telemetry.ShouldRecordRequest(context));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("  ", false)]
    [InlineData("InstrumentationKey=00000000-0000-0000-0000-000000000000", true)]
    // No connection string (local, compose, CI) = nothing is sent anywhere.
    public void IsEnabled_OnlyWithAConnectionString(string? connectionString, bool enabled)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [Telemetry.ConnectionStringKey] = connectionString })
            .Build();

        Assert.Equal(enabled, Telemetry.IsEnabled(configuration));
    }
}
