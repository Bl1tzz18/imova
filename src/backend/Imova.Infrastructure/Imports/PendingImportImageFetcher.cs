using Imova.Application.Common.Interfaces;

namespace Imova.Infrastructure.Imports;

// Stand-in until the hardened downloader (step 4 of the CSV import: private / local addresses refused
// on the address actually connected to, redirects re-checked, size and time limits) is in: downloads
// nothing, so every imported row fails on its photos. Never register this outside the Worker.
public sealed class PendingImportImageFetcher : IImportImageFetcher
{
    public Task<ImportImageDownload> DownloadAsync(string url, CancellationToken cancellationToken) =>
        Task.FromResult(ImportImageDownload.Failed(ImportImageFailure.Blocked));
}
