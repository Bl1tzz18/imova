using Imova.Application.Common.Interfaces;
using Imova.Domain.Listings;

namespace Imova.Application.Features.Media.Sizes;

public enum PhotoSizeOutcome
{
    Generated,

    // The original isn't in storage (deleted, or the upload never landed).
    SourceMissing,

    // The original isn't an image ImageMagick can read (corrupt, or only pretends to be one).
    Unreadable,
}

// Makes a photo's display copies (see PhotoSizes) from its original and marks the photo; the
// caller saves. Writing a copy that already exists overwrites it, so running it twice is harmless.
public class PhotoSizeGenerator(IBlobStorageService blobStorageService, IPhotoResizer photoResizer)
{
    private static readonly IReadOnlyList<int> LongestSides = PhotoSizes.All.Select(PhotoSizes.LongestSide).ToList();

    public async Task<PhotoSizeOutcome> GenerateAsync(Photo photo, CancellationToken cancellationToken)
    {
        // Read fully first: the storage stream isn't seekable, and the original is at most 10 MB.
        using var original = new MemoryStream();
        await using (var source = await blobStorageService.OpenAsync(photo.BlobName, cancellationToken))
        {
            if (source is null)
            {
                return PhotoSizeOutcome.SourceMissing;
            }

            await source.CopyToAsync(original, cancellationToken);
        }

        original.Position = 0;
        var copies = await photoResizer.ResizeToJpegAsync(original, LongestSides, cancellationToken);
        if (copies is null)
        {
            return PhotoSizeOutcome.Unreadable;
        }

        for (var i = 0; i < PhotoSizes.All.Count; i++)
        {
            using var content = new MemoryStream(copies[i]);
            await blobStorageService.UploadAsync(PhotoSizes.BlobName(photo.BlobName, PhotoSizes.All[i]), content, "image/jpeg", cancellationToken);
        }

        photo.MarkSizesGenerated(PhotoSizes.CurrentVersion);
        return PhotoSizeOutcome.Generated;
    }
}
