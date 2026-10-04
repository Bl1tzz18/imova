namespace Imova.Application.Common.Interfaces;

// Turns an uploaded photo into smaller JPEG copies for display (see PhotoSizes). Implemented with
// ImageMagick in Infrastructure (MagickPhotoResizer); Application only sees bytes in, bytes out.
public interface IPhotoResizer
{
    // One JPEG per requested longest side, in the same order. Each copy is turned upright (EXIF
    // orientation), converted to sRGB, stripped of metadata (camera details, GPS) and never made
    // larger than the source. Null when the source can't be read as an image.
    Task<IReadOnlyList<byte[]>?> ResizeToJpegAsync(Stream source, IReadOnlyList<int> longestSides, CancellationToken cancellationToken);

    // A logo: the same clean-up, then for each size a square JPEG of exactly that size, the image
    // fitted inside it (shrunk or enlarged, aspect ratio kept) and centred on white. Null when the
    // source can't be read as an image.
    Task<IReadOnlyList<byte[]>?> ResizeToSquareJpegAsync(Stream source, IReadOnlyList<int> sides, CancellationToken cancellationToken);

    // The image's width and height as stored, from its header alone (no pixels decoded); null when
    // it can't be read as an image.
    (int Width, int Height)? ReadSize(byte[] source);
}
