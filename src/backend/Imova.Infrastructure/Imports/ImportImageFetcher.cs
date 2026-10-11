using System.Net;
using System.Net.Sockets;
using Imova.Application.Common.Interfaces;
using Imova.Domain.Listings;
using Microsoft.Extensions.Logging;

namespace Imova.Infrastructure.Imports;

// Downloads the photos named in an agency's CSV — addresses from outside, so it must never be a way
// into IMOVA's own network:
// - every connection's destination is checked when it's made, on the IP address actually connected
//   to (so a name that resolves to a public address on a first lookup and to an internal one on the
//   next — DNS rebinding — is caught), against PublicAddress; literal IPs the same way;
// - redirects are followed by hand, at most MaxRedirects, each new address checked again (only
//   http/https, no user:password@);
// - no proxy (it would connect for us, past the check); no cookies or credentials;
// - a size cap (Photo.MaxFileSizeBytes) checked on Content-Length and while reading, and one time
//   limit for the whole download, redirects included.
// Never throws for a bad address or server; cancellation by the caller still throws.
public sealed class ImportImageFetcher : IImportImageFetcher, IDisposable
{
    public const int MaxRedirects = 3;
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);

    private readonly HttpClient _client;
    private readonly ILogger<ImportImageFetcher> _logger;
    private readonly TimeSpan _timeout;
    private readonly long _maxBytes;

    public ImportImageFetcher(ILogger<ImportImageFetcher> logger)
        : this(logger, PublicAddress.IsPublic, DefaultTimeout, Photo.MaxFileSizeBytes)
    {
    }

    // isAllowed / limits replaceable for tests (a local test server is on a loopback address).
    public ImportImageFetcher(ILogger<ImportImageFetcher> logger, Func<IPAddress, bool> isAllowed, TimeSpan timeout, long maxBytes)
    {
        _logger = logger;
        _timeout = timeout;
        _maxBytes = maxBytes;

        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.None,
            ConnectTimeout = TimeSpan.FromSeconds(10),
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            ConnectCallback = (context, cancellationToken) => ConnectAsync(context, isAllowed, cancellationToken),
        };
        _client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("IMOVA-import/1.0 (+https://imova.md)");
        _client.DefaultRequestHeaders.Accept.ParseAdd("image/*");
    }

    public async Task<ImportImageDownload> DownloadAsync(string url, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);

        try
        {
            var current = url;
            for (var redirects = 0; redirects <= MaxRedirects; redirects++)
            {
                if (!Uri.TryCreate(current, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                    || !string.IsNullOrEmpty(uri.UserInfo))
                {
                    return ImportImageDownload.Failed(ImportImageFailure.Blocked);
                }

                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

                if (IsRedirect(response.StatusCode))
                {
                    if (response.Headers.Location is not { } location)
                    {
                        return ImportImageDownload.Failed(ImportImageFailure.Failed);
                    }

                    current = (location.IsAbsoluteUri ? location : new Uri(uri, location)).ToString();
                    continue;
                }

                if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                {
                    return ImportImageDownload.Failed(ImportImageFailure.NotFound);
                }

                if (!response.IsSuccessStatusCode)
                {
                    return ImportImageDownload.Failed(ImportImageFailure.Failed);
                }

                if (response.Content.Headers.ContentLength > _maxBytes)
                {
                    return ImportImageDownload.Failed(ImportImageFailure.TooLarge);
                }

                var content = await ReadCappedAsync(response.Content, timeout.Token);
                return content is null ? ImportImageDownload.Failed(ImportImageFailure.TooLarge) : ImportImageDownload.Of(content);
            }

            return ImportImageDownload.Failed(ImportImageFailure.Failed);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ImportImageDownload.Failed(ImportImageFailure.TimedOut);
        }
        catch (HttpRequestException ex) when (FindBlocked(ex) is { } blocked)
        {
            _logger.LogWarning("Import photo {Url} refused: {Reason}", url, blocked.Message);
            return ImportImageDownload.Failed(ImportImageFailure.Blocked);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogInformation(ex, "Import photo {Url} could not be downloaded.", url);
            return ImportImageDownload.Failed(ImportImageFailure.Failed);
        }
    }

    public void Dispose() => _client.Dispose();

    private static bool IsRedirect(HttpStatusCode status) =>
        status is HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;

    // The body, or null once it grows past the cap (Content-Length can be absent or wrong).
    private async Task<byte[]?> ReadCappedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > _maxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    // Resolves the name (or takes the literal IP) and connects only to an allowed address.
    private static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, Func<IPAddress, bool> isAllowed, CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        IPAddress[] addresses = IPAddress.TryParse(host.Trim('[', ']'), out var literal)
            ? new[] { literal }
            : await Dns.GetHostAddressesAsync(host, cancellationToken);

        var allowed = addresses.Where(isAllowed).ToList();
        if (allowed.Count == 0)
        {
            throw new BlockedDestinationException($"{host} resolves to no public address.");
        }

        Exception? last = null;
        foreach (var address in allowed)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException ex)
            {
                socket.Dispose();
                last = ex;
            }
        }

        throw last!;
    }

    private static BlockedDestinationException? FindBlocked(Exception exception)
    {
        for (var current = (Exception?)exception; current is not null; current = current.InnerException)
        {
            if (current is BlockedDestinationException blocked)
            {
                return blocked;
            }
        }

        return null;
    }

    private sealed class BlockedDestinationException(string message) : Exception(message);
}
