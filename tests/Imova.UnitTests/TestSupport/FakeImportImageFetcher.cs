using Imova.Application.Common.Interfaces;

namespace Imova.UnitTests.TestSupport;

// Every address downloads a tiny "JPEG" (its first bytes are JPEG's signature, the rest the address)
// unless a failure or other bytes are set for it. Counts the downloads.
internal sealed class FakeImportImageFetcher : IImportImageFetcher
{
    public Dictionary<string, ImportImageFailure> Failures { get; } = [];

    public Dictionary<string, byte[]> Content { get; } = [];

    public List<string> Downloaded { get; } = [];

    public static byte[] Jpeg(string url) => [0xFF, 0xD8, 0xFF, 0xE0, .. System.Text.Encoding.UTF8.GetBytes(url)];

    public Task<ImportImageDownload> DownloadAsync(string url, CancellationToken cancellationToken)
    {
        Downloaded.Add(url);
        if (Failures.TryGetValue(url, out var failure))
        {
            return Task.FromResult(ImportImageDownload.Failed(failure));
        }

        return Task.FromResult(ImportImageDownload.Of(Content.GetValueOrDefault(url) ?? Jpeg(url)));
    }
}
