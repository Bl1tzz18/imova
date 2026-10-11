using System.Net;
using System.Net.Sockets;
using Imova.Application.Common.Interfaces;
using Imova.Infrastructure.Imports;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Imports;

// Which addresses an import may download photos from: the public internet only.
public class PublicAddressTests
{
    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("185.82.216.10")]
    [InlineData("2a00:1450:4001:80b::200e")]
    [InlineData("::ffff:8.8.8.8")]
    [InlineData("2002:0808:0808::1")]
    [InlineData("64:ff9b::808:808")]
    public void PublicAddresses_AreAllowed(string address) => Assert.True(PublicAddress.IsPublic(IPAddress.Parse(address)));

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0.2")]
    [InlineData("10.1.2.3")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("192.0.2.10")]
    [InlineData("198.18.0.1")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("::1")]
    [InlineData("::")]
    [InlineData("fe80::1")]
    [InlineData("fd00::1")]
    [InlineData("ff02::1")]
    [InlineData("2001:db8::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:169.254.169.254")]
    [InlineData("2002:0a00:0001::1")]
    [InlineData("64:ff9b::a00:1")]
    public void PrivateLocalAndReservedAddresses_AreRefused(string address) => Assert.False(PublicAddress.IsPublic(IPAddress.Parse(address)));

    [Fact]
    public void TheEdgesOfARange_AreExact()
    {
        Assert.True(PublicAddress.IsPublic(IPAddress.Parse("172.15.255.255")));
        Assert.True(PublicAddress.IsPublic(IPAddress.Parse("172.32.0.0")));
        Assert.True(PublicAddress.IsPublic(IPAddress.Parse("100.128.0.0")));
    }
}

// The downloader against a small HTTP server on this machine. Its own address (127.0.0.1) is allowed
// in these tests through the replaceable check; 127.0.0.2 stands for "anything internal".
public sealed class ImportImageFetcherTests : IDisposable
{
    private readonly HttpListener _server = new();
    private readonly int _port;
    private readonly Dictionary<string, Func<HttpListenerResponse, Task>> _routes = [];
    private readonly List<string> _requested = [];

    public ImportImageFetcherTests()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        _port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        _server.Prefixes.Add($"http://127.0.0.1:{_port}/");
        _server.Start();
        _ = ServeAsync();
    }

    public void Dispose() => _server.Close();

    private string Url(string path) => $"http://127.0.0.1:{_port}{path}";

    private async Task ServeAsync()
    {
        while (_server.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _server.GetContextAsync();
            }
            catch (Exception)
            {
                return;
            }

            var path = context.Request.Url!.AbsolutePath;
            lock (_requested)
            {
                _requested.Add(path);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    if (_routes.TryGetValue(path, out var route))
                    {
                        await route(context.Response);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                    }

                    context.Response.Close();
                }
                catch (Exception)
                {
                    // The client went away (a timeout or a size cap) — fine.
                }
            });
        }
    }

    private static Func<HttpListenerResponse, Task> Bytes(int count, bool chunked = false) => async response =>
    {
        response.ContentType = "image/jpeg";
        response.SendChunked = chunked;
        if (!chunked)
        {
            response.ContentLength64 = count;
        }

        await response.OutputStream.WriteAsync(new byte[count]);
    };

    private static Func<HttpListenerResponse, Task> Redirect(string location) => response =>
    {
        response.StatusCode = 302;
        response.RedirectLocation = location;
        return Task.CompletedTask;
    };

    private static ImportImageFetcher LocalOnly(TimeSpan? timeout = null, long maxBytes = 1000) =>
        new(NullLogger<ImportImageFetcher>.Instance, ip => ip.Equals(IPAddress.Loopback), timeout ?? TimeSpan.FromSeconds(5), maxBytes);

    private static ImportImageFetcher Default() => new(NullLogger<ImportImageFetcher>.Instance);

    [Fact]
    public async Task APhoto_IsDownloaded_FollowingRedirectsToAllowedAddresses()
    {
        _routes["/a.jpg"] = Redirect("/b.jpg");
        _routes["/b.jpg"] = Bytes(600);

        using var fetcher = LocalOnly();
        var download = await fetcher.DownloadAsync(Url("/a.jpg"), CancellationToken.None);

        Assert.Null(download.Failure);
        Assert.Equal(600, download.Content!.Length);
    }

    [Fact]
    public async Task MissingAndBrokenPhotos_AreReportedAsSuch()
    {
        _routes["/broken.jpg"] = response =>
        {
            response.StatusCode = 500;
            return Task.CompletedTask;
        };

        using var fetcher = LocalOnly();

        Assert.Equal(ImportImageFailure.NotFound, (await fetcher.DownloadAsync(Url("/nope.jpg"), CancellationToken.None)).Failure);
        Assert.Equal(ImportImageFailure.Failed, (await fetcher.DownloadAsync(Url("/broken.jpg"), CancellationToken.None)).Failure);
    }

    [Fact]
    public async Task ATooLargePhoto_IsRefused_WhetherItSaysSoOrNot()
    {
        _routes["/declared.jpg"] = Bytes(2000);
        _routes["/chunked.jpg"] = Bytes(2000, chunked: true);

        using var fetcher = LocalOnly(maxBytes: 1000);

        Assert.Equal(ImportImageFailure.TooLarge, (await fetcher.DownloadAsync(Url("/declared.jpg"), CancellationToken.None)).Failure);
        Assert.Equal(ImportImageFailure.TooLarge, (await fetcher.DownloadAsync(Url("/chunked.jpg"), CancellationToken.None)).Failure);
    }

    [Fact]
    public async Task ARedirectToAnInternalAddress_OrAnotherScheme_IsBlocked()
    {
        _routes["/to-internal.jpg"] = Redirect($"http://127.0.0.2:{_port}/secret");
        _routes["/to-file.jpg"] = Redirect("file:///etc/passwd");

        using var fetcher = LocalOnly();

        Assert.Equal(ImportImageFailure.Blocked, (await fetcher.DownloadAsync(Url("/to-internal.jpg"), CancellationToken.None)).Failure);
        Assert.Equal(ImportImageFailure.Blocked, (await fetcher.DownloadAsync(Url("/to-file.jpg"), CancellationToken.None)).Failure);
        Assert.DoesNotContain("/secret", _requested);
    }

    [Fact]
    public async Task EndlessRedirects_Fail_AfterThree()
    {
        _routes["/loop.jpg"] = Redirect("/loop.jpg");

        using var fetcher = LocalOnly();
        var download = await fetcher.DownloadAsync(Url("/loop.jpg"), CancellationToken.None);

        Assert.Equal(ImportImageFailure.Failed, download.Failure);
        Assert.Equal(ImportImageFetcher.MaxRedirects + 1, _requested.Count(p => p == "/loop.jpg"));
    }

    [Fact]
    public async Task ASlowServer_TimesOut()
    {
        _routes["/slow.jpg"] = async response =>
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
            await Bytes(10)(response);
        };

        using var fetcher = LocalOnly(timeout: TimeSpan.FromMilliseconds(300));

        Assert.Equal(ImportImageFailure.TimedOut, (await fetcher.DownloadAsync(Url("/slow.jpg"), CancellationToken.None)).Failure);
    }

    [Theory]
    [InlineData("http://127.0.0.1:{port}/a.jpg")]
    [InlineData("http://localhost:{port}/a.jpg")]
    [InlineData("http://[::1]:{port}/a.jpg")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://10.0.0.1/a.jpg")]
    [InlineData("http://user:secret@example.com/a.jpg")]
    [InlineData("ftp://example.com/a.jpg")]
    public async Task ByDefault_LocalPrivateAndMetadataAddresses_AreNeverContacted(string url)
    {
        _routes["/a.jpg"] = Bytes(10);

        using var fetcher = Default();
        var download = await fetcher.DownloadAsync(url.Replace("{port}", _port.ToString()), CancellationToken.None);

        Assert.Equal(ImportImageFailure.Blocked, download.Failure);
        Assert.Empty(_requested);
    }

    [Fact]
    public async Task CancellingTheImport_StillCancels()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        using var fetcher = LocalOnly();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fetcher.DownloadAsync(Url("/a.jpg"), cancelled.Token));
    }
}
