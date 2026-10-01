using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Media.Sizes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Media.Cleanup;

// One run of the photo-cleanup job (the Worker runs it periodically). The add-listing form uploads
// photos under a client-generated listing id before the listing exists (see Photo); when the form
// is abandoned, those photos never get a listing. After AbandonedAfter they're deleted — the files
// (original and sizes) first, then the row, so a failed file delete is simply retried on the next run. Photos of saved
// drafts are untouched: a Draft is a real listing row.
public class AbandonedPhotoCleanup(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    TimeProvider timeProvider,
    ILogger<AbandonedPhotoCleanup> logger) : IScheduledJob
{
    // Long enough that nobody is still filling in the form.
    public static readonly TimeSpan AbandonedAfter = TimeSpan.FromDays(7);

    public const int BatchSize = 500;

    // Returns how many photos were deleted.
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow() - AbandonedAfter;

        var abandoned = await dbContext.Photos
            .Where(p => p.CreatedAt < cutoff && !dbContext.Listings.Any(l => l.Id == p.ListingId))
            .OrderBy(p => p.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var deleted = 0;
        foreach (var photo in abandoned)
        {
            try
            {
                foreach (var blobName in PhotoSizes.AllBlobNames(photo))
                {
                    await blobStorageService.DeleteAsync(blobName, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not delete abandoned photo blob {BlobName}; retrying next run.", photo.BlobName);
                continue;
            }

            dbContext.Photos.Remove(photo);
            deleted++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return deleted;
    }
}
