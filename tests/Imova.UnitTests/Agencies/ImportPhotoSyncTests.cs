using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Agencies.Import;
using Imova.Application.Features.Media.Sizes;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Agencies;

// A listing's photos become what its row lists, in that order; unchanged addresses aren't
// downloaded again; too few usable photos change nothing.
public class ImportPhotoSyncTests : IAsyncDisposable
{
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly FakeBlobStorageService _blobs = new();
    private readonly FakeImportImageFetcher _fetcher = new();
    private readonly FakePhotoResizer _resizer = new();
    private readonly Guid _listingId = Guid.NewGuid();
    private readonly Guid _uploader = Guid.NewGuid();

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private ImportPhotoSync Sync() =>
        new(_db, _blobs, _fetcher, new PhotoSizeGenerator(_blobs, _resizer), NullLogger<ImportPhotoSync>.Instance);

    private static string[] Urls(params int[] numbers) => numbers.Select(n => $"https://agentie.md/foto/{n}.jpg").ToArray();

    private async Task<ImportPhotoResult> SyncAndSaveAsync(params string[] urls)
    {
        var result = await Sync().SyncAsync(_listingId, urls, _uploader, CancellationToken.None);
        await _db.SaveChangesAsync();
        return result;
    }

    private Task<List<Photo>> PhotosAsync() => _db.Photos.Where(p => p.ListingId == _listingId).OrderBy(p => p.SortOrder).ToListAsync();

    [Fact]
    public async Task NewAddresses_AreDownloaded_StoredInOrder_TheFirstAsTheCover()
    {
        var urls = Urls(1, 2, 3);

        var result = await SyncAndSaveAsync(urls);

        Assert.True(result.Enough);
        Assert.Empty(result.Problems);
        var photos = await PhotosAsync();
        Assert.Equal(urls, photos.Select(p => p.SourceUrl));
        Assert.Equal([true, false, false], photos.Select(p => p.IsPrimary));
        Assert.All(photos, p => Assert.Equal((_uploader, "image/jpeg"), (p.UploadedByUserId!.Value, p.ContentType)));
        Assert.All(photos, p => Assert.Contains(p.BlobName, _blobs.StoredBlobNames));
        Assert.All(photos, p => Assert.True(p.SizesVersion > 0));
    }

    [Fact]
    public async Task AnAddressThatCantBeUsed_IsAWarning_WhileThreeRemain()
    {
        var urls = Urls(1, 2, 3, 4, 5);
        _fetcher.Failures[urls[1]] = ImportImageFailure.NotFound;
        _fetcher.Content[urls[3]] = "<html>nu e o poză</html>"u8.ToArray();

        var result = await SyncAndSaveAsync(urls);

        Assert.True(result.Enough);
        Assert.Equal(3, result.Count);
        Assert.Equal(
            [("NotFound", urls[1]), ("NotAnImage", urls[3])],
            result.Problems.Select(p => (p.Params!["reason"], p.Params["url"])));
        Assert.All(result.Problems, p => Assert.Equal((ListingImportProblemSeverity.Warning, ErrorCodes.ImportPhotoFailed), (p.Severity, p.Code)));
        Assert.Equal([urls[0], urls[2], urls[4]], (await PhotosAsync()).Select(p => p.SourceUrl));
    }

    [Fact]
    public async Task FewerThanThreeUsable_ChangesNothing_AndRemovesWhatItDownloaded()
    {
        var urls = Urls(1, 2, 3);
        _fetcher.Failures[urls[2]] = ImportImageFailure.Blocked;

        var result = await SyncAndSaveAsync(urls);

        Assert.False(result.Enough);
        Assert.Contains(result.Problems, p => p is { Severity: ListingImportProblemSeverity.Error, Code: ErrorCodes.ImportNotEnoughPhotos });
        Assert.Empty(await PhotosAsync());
        // The two downloaded originals and their display sizes.
        Assert.Equal(2 * (1 + PhotoSizes.All.Count), _blobs.DeletedBlobNames.Count);
    }

    [Fact]
    public async Task AReImport_KeepsUnchangedAddresses_DropsRemovedOnes_AndKeepsHandUploadedPhotosLast()
    {
        await SyncAndSaveAsync(Urls(1, 2, 3));
        ListingTestData.AddPhotos(_db, _listingId, count: 1);
        await _db.SaveChangesAsync();
        var handUploaded = (await PhotosAsync()).Single(p => p.SourceUrl is null);
        _fetcher.Downloaded.Clear();

        var result = await SyncAndSaveAsync(Urls(3, 4, 1));

        Assert.True(result.Enough);
        Assert.Equal([Urls(4)[0]], _fetcher.Downloaded);
        var photos = await PhotosAsync();
        Assert.Equal([.. Urls(3, 4, 1), null], photos.Select(p => p.SourceUrl));
        Assert.Equal(handUploaded.Id, photos[^1].Id);
        Assert.True(photos[0].IsPrimary);
        Assert.Single(photos, p => p.IsPrimary);
    }
}
