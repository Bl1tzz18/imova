using System.Net;
using Imova.Api.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Imova.IntegrationTests.Common;

// Runs ASP.NET's own ForwardedHeadersMiddleware with the options AuthRateLimiting builds, for the
// two deployments: compose (one proxy) and Azure Container Apps (ingress + web app). No server or
// database needed.
public class ForwardedHeadersTests
{
    private static readonly Dictionary<string, string?> AzureSettings = new()
    {
        ["ForwardedHeaders:KnownNetworks:0"] = "10.0.0.0/8",
        ["ForwardedHeaders:KnownNetworks:1"] = "172.16.0.0/12",
        ["ForwardedHeaders:KnownNetworks:2"] = "192.168.0.0/16",
        ["ForwardedHeaders:KnownNetworks:3"] = "100.64.0.0/10",
        ["ForwardedHeaders:ForwardLimit"] = "2",
    };

    private static async Task<string?> ClientIpAsync(Dictionary<string, string?> settings, string remoteIp, string? forwardedFor)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var options = new ForwardedHeadersOptions();
        AuthRateLimiting.ConfigureForwardedHeaders(options, configuration);

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        if (forwardedFor is not null)
        {
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        }

        var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(options));
        await middleware.Invoke(context);
        return context.Connection.RemoteIpAddress?.ToString();
    }

    [Fact]
    public async Task Azure_server_action_call_resolves_to_the_visitor()
    {
        // The web app forwarded the visitor; the ingress appended the web app's address.
        Assert.Equal("203.0.113.7", await ClientIpAsync(AzureSettings, "100.100.1.5", "203.0.113.7, 100.100.2.9"));
    }

    [Fact]
    public async Task Azure_direct_browser_call_resolves_to_the_client()
    {
        Assert.Equal("198.51.100.20", await ClientIpAsync(AzureSettings, "100.100.1.5", "198.51.100.20"));
    }

    [Fact]
    public async Task Azure_spoofed_entry_is_ignored()
    {
        // The client sent "1.2.3.4" itself; the ingress appended the client's real address.
        Assert.Equal("198.51.100.20", await ClientIpAsync(AzureSettings, "100.100.1.5", "1.2.3.4, 198.51.100.20"));
    }

    [Fact]
    public async Task Header_from_an_untrusted_address_is_ignored()
    {
        Assert.Equal("198.51.100.20", await ClientIpAsync(AzureSettings, "198.51.100.20", "1.2.3.4"));
    }

    [Fact]
    public async Task Compose_default_limit_takes_the_one_entry_the_web_app_sent()
    {
        var compose = new Dictionary<string, string?> { ["ForwardedHeaders:KnownNetworks:0"] = "172.16.0.0/12" };

        Assert.Equal("203.0.113.7", await ClientIpAsync(compose, "172.18.0.5", "203.0.113.7"));
        // Without ForwardLimit only the rightmost entry counts — why Azure needs the limit of 2.
        Assert.Equal("172.18.0.9", await ClientIpAsync(compose, "172.18.0.5", "203.0.113.7, 172.18.0.9"));
    }
}
