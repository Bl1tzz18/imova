using System.Globalization;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Application.Features.Media.Sizes;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Agencies.Import;

public sealed record ImportPhotoResult(bool Enough, int Count, IReadOnlyList<ListingImportProblem> Problems);

// Makes a listing's photos what its CSV row lists, in that order (the first is the cover):
// - an address already imported for it (Photo.SourceUrl) is kept, not downloaded again;
// - a new address is downloaded (IImportImageFetcher), checked to really be an image the same way an
//   upload is (its signature, then the display sizes), and stored like an upload;
// - imported photos whose address is no longer listed are removed; photos uploaded by hand stay,
//   after the imported ones.
// If that would leave fewer than ListingPhotoRules.MinPhotos, nothing changes (new downloads are
// removed again) and the row fails. An address that can't be used is a warning on the row.
// The photo rows are added to / removed from the context for the caller's save; blobs are written
// at once (and deleted again when the row fails).
public sealed class ImportPhotoSync(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    IImportImageFetcher fetcher,
    PhotoSizeGenerator photoSizeGenerator,
    ILogger<ImportPhotoSync> logger)
{
    // The photos the last SyncAsync downloaded, until the caller saved the row (CommitDownloads) or
    // gave up on it (DiscardDownloadsAsync — their blobs would otherwise be left behind).
    private List<Photo> _pending = [];

    public void CommitDownloads() => _pending = [];

    // Best effort: a blob that can't be deleted is logged and left.
    public async Task DiscardDownloadsAsync(CancellationToken cancellationToken)
    {
        var pending = _pending;
        _pending = [];
        if (pending.Count > 0)
        {
            logger.LogInformation("Removing {Count} photos downloaded for an import row that wasn't saved.", pending.Count);
            await DeleteBlobsAsync(pending, cancellationToken);
        }
    }

    public async Task<ImportPhotoResult> SyncAsync(Guid listingId, IReadOnlyList<string> urls, Guid uploadedByUserId, CancellationToken cancellationToken)
    {
        _pending = [];
        var existing = await dbContext.Photos.Where(p => p.ListingId == listingId).OrderBy(p => p.SortOrder).ToListAsync(cancellationToken);
        var importedByUrl = existing.Where(p => p.SourceUrl is not null).GroupBy(p => p.SourceUrl!).ToDictionary(g => g.Key, g => g.First());
        var manual = existing.Where(p => p.SourceUrl is null).ToList();

        var problems = new List<ListingImportProblem>();
        var ordered = new List<Photo>();
        var downloaded = new List<Photo>();
        foreach (var url in urls)
        {
            if (importedByUrl.TryGetValue(url, out var kept))
            {
                ordered.Add(kept);
                continue;
            }

            var (photo, failure) = await DownloadAsync(listingId, url, uploadedByUserId, cancellationToken);
            if (photo is null)
            {
                problems.Add(ImportProblems.Warning(
                    ImportColumns.ImageUrls, ErrorCodes.ImportPhotoFailed, $"Photo {url} wasn't used ({failure}).", ("url", url), ("reason", failure!)));
                continue;
            }

            ordered.Add(photo);
            downloaded.Add(photo);
        }

        var count = ordered.Count + manual.Count;
        if (count < ListingPhotoRules.MinPhotos)
        {
            await DeleteBlobsAsync(downloaded, cancellationToken);
            problems.Add(ImportProblems.Error(
                ImportColumns.ImageUrls,
                ErrorCodes.ImportNotEnoughPhotos,
                $"Only {count} usable photos; at least {ListingPhotoRules.MinPhotos} are needed.",
                ("min", ListingPhotoRules.MinPhotos.ToString(CultureInfo.InvariantCulture)),
                ("count", count.ToString(CultureInfo.InvariantCulture))));
            return new ImportPhotoResult(false, count, problems);
        }

        var removed = existing.Where(p => p.SourceUrl is not null && !ordered.Contains(p)).ToList();
        dbContext.Photos.RemoveRange(removed);
        dbContext.Photos.AddRange(downloaded);

        var position = 0;
        foreach (var photo in ordered.Concat(manual))
        {
            photo.PlaceAt(position, isPrimary: position == 0);
            position++;
        }

        _pending = downloaded;
        await DeleteBlobsAsync(removed, cancellationToken);
        return new ImportPhotoResult(true, count, problems);
    }

    private async Task<(Photo? Photo, string? Failure)> DownloadAsync(Guid listingId, string url, Guid uploadedByUserId, CancellationToken cancellationToken)
    {
        var download = await fetcher.DownloadAsync(url, cancellationToken);
        if (download.Content is not { } content)
        {
            return (null, download.Failure?.ToString() ?? nameof(ImportImageFailure.Failed));
        }

        if (content.Length > Photo.MaxFileSizeBytes)
        {
            return (null, nameof(ImportImageFailure.TooLarge));
        }

        var contentType = ImageSignature.DetectContentType(content);
        if (contentType is null || ImageSignature.ExtensionForContentType(contentType) is not { } extension)
        {
            return (null, "NotAnImage");
        }

        var blobName = blobStorageService.GenerateBlobName(listingId, extension);
        using (var stream = new MemoryStream(content, writable: false))
        {
            await blobStorageService.UploadAsync(blobName, stream, contentType, cancellationToken);
        }

        var photo = Photo.Create(listingId, blobName, contentType, content.Length, uploadedByUserId: uploadedByUserId, sourceUrl: url);
        try
        {
            if (await photoSizeGenerator.GenerateAsync(photo, cancellationToken) == PhotoSizeOutcome.Unreadable)
            {
                await DeleteBlobsAsync([photo], cancellationToken);
                return (null, "NotAnImage");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Like an upload: the Worker's backfill makes the sizes later; the original is shown until then.
            logger.LogWarning(ex, "Could not make the display sizes of imported photo {BlobName}; the backfill will.", blobName);
        }

        return (photo, null);
    }

    private async Task DeleteBlobsAsync(IEnumerable<Photo> photos, CancellationToken cancellationToken)
    {
        foreach (var blobName in photos.SelectMany(PhotoSizes.AllBlobNames))
        {
            try
            {
                await blobStorageService.DeleteAsync(blobName, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not delete photo blob {BlobName}.", blobName);
            }
        }
    }
}
