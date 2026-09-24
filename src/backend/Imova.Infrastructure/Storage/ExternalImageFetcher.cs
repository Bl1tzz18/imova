using Imova.Application.Common.Interfaces;
using Imova.Domain.Listings;

namespace Imova.Infrastructure.Storage;

public class ExternalImageFetcher(HttpClient httpClient) : IExternalImageFetcher
{
    public async Task<byte[]?> TryDownloadAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            if (response.Content.Headers.ContentLength is { } declaredLength &&
                declaredLength > Photo.MaxFileSizeBytes)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var buffer = new MemoryStream();

            // Cap the actual bytes read too — a server can omit or lie about Content-Length.
            var chunk = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
            {
                if (buffer.Length + read > Photo.MaxFileSizeBytes)
                {
                    return null;
                }

                buffer.Write(chunk, 0, read);
            }

            return buffer.Length > 0 ? buffer.ToArray() : null;
        }
        catch
        {
            // Network errors, timeouts, malformed URLs, etc. — the caller decides whether/how to
            // log; this just signals "couldn't get it".
            return null;
        }
    }
}
