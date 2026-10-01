using Imova.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Media.Sizes;

// One run of the photo-sizes job (the Worker runs it periodically): makes the display copies for
// photos that don't have the current version yet — photos from before sizes existed, ones whose
// copies failed at upload for a passing reason (storage briefly unreachable), and every photo
// again after PhotoSizes.CurrentVersion is bumped. Newest first, since those are the most likely
// to be on screen. A photo whose original is gone or unreadable is marked and left alone (it
// keeps showing the original, as before).
public class PhotoSizeBackfill(
    IApplicationDbContext dbContext,
    PhotoSizeGenerator generator,
    TimeProvider timeProvider,
    ILogger<PhotoSizeBackfill> logger) : IScheduledJob
{
    public const int BatchSize = 100;

    // Returns how many photos got their copies.
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var pending = await dbContext.Photos
            .Where(p => p.SizesVersion < PhotoSizes.CurrentVersion && p.SizesFailedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var generated = 0;
        foreach (var photo in pending)
        {
            PhotoSizeOutcome outcome;
            try
            {
                outcome = await generator.GenerateAsync(photo, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not make the display sizes of photo {PhotoId}; retrying next run.", photo.Id);
                continue;
            }

            if (outcome == PhotoSizeOutcome.Generated)
            {
                generated++;
            }
            else
            {
                logger.LogWarning("Photo {PhotoId} gets no display sizes: {Outcome}.", photo.Id, outcome);
                photo.MarkSizesFailed(timeProvider.GetUtcNow());
            }
        }

        // One save for the batch. If a photo was deleted meanwhile the save fails and the batch is
        // simply done again next run (making the copies again is harmless).
        await dbContext.SaveChangesAsync(cancellationToken);
        return generated;
    }
}
