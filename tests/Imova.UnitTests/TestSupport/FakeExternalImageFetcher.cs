using Imova.Application.Common.Interfaces;

namespace Imova.UnitTests.TestSupport;

internal sealed class FakeExternalImageFetcher : IExternalImageFetcher
{
    public byte[]? BytesToReturn { get; set; }

    public Task<byte[]?> TryDownloadAsync(string url, CancellationToken cancellationToken) =>
        Task.FromResult(BytesToReturn);
}
