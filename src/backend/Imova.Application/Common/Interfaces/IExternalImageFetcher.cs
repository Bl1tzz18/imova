namespace Imova.Application.Common.Interfaces;

// Downloads an image from a URL we don't control (e.g. a Google profile picture) so it can be
// re-uploaded into our own blob storage — see GoogleLoginHandler. Never throws: a failure here
// (network error, non-2xx, timeout, oversized response) must not block whatever the caller was
// doing, so it returns null instead.
public interface IExternalImageFetcher
{
    Task<byte[]?> TryDownloadAsync(string url, CancellationToken cancellationToken);
}
