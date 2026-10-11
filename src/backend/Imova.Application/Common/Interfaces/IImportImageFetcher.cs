namespace Imova.Application.Common.Interfaces;

// Why a photo address from an import couldn't be downloaded — the report shows it per address.
public enum ImportImageFailure
{
    // An address that resolves to a private / local network (or isn't http(s)) — never fetched.
    Blocked = 1,
    NotFound = 2,
    TooLarge = 3,
    TimedOut = 4,
    // Anything else: refused connection, a 5xx, too many redirects, …
    Failed = 5,
}

public sealed record ImportImageDownload(byte[]? Content, ImportImageFailure? Failure)
{
    public static ImportImageDownload Of(byte[] content) => new(content, null);

    public static ImportImageDownload Failed(ImportImageFailure failure) => new(null, failure);
}

// Downloads the photos an agency's CSV names (public http(s) addresses on the internet). The
// implementation must refuse private / loopback / link-local destinations (checked on the address
// actually connected to, every redirect included) and enforce size and time limits — see
// Infrastructure. Never throws for a bad address; cancellation still throws.
public interface IImportImageFetcher
{
    Task<ImportImageDownload> DownloadAsync(string url, CancellationToken cancellationToken);
}
