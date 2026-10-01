using Imova.Application.Common.Interfaces;
using Imova.Domain.Listings;

namespace Imova.Application.Features.Media.Sizes;

public enum PhotoSize
{
    // Small previews: gallery strips, "Anunțurile mele", moderation rows, map popups.
    Thumbnail,

    // Listing cards in search results and carousels, the alert email.
    Card,

    // The listing page and the full-screen viewer.
    Large,
}

// The display copies of a listing photo. The uploaded original stays untouched as the source (and
// is what the personal-data export contains); next to it, under derived names, live JPEGs no
// larger than these longest sides: "{listingId}/{guid}.png" → "{listingId}/{guid}_400.jpg", _800,
// _1600. They're made when the photo is confirmed (PhotoSizeGenerator) or, for older photos and
// failed attempts, by the Worker (PhotoSizeBackfill).
public static class PhotoSizes
{
    // Bump when the sizes or the encoding change: the backfill then remakes every photo's copies.
    // A version that adds a size must still read older photos' URLs from the sizes they do have.
    public const int CurrentVersion = 1;

    public static readonly IReadOnlyList<PhotoSize> All = [PhotoSize.Thumbnail, PhotoSize.Card, PhotoSize.Large];

    public static int LongestSide(PhotoSize size) => size switch
    {
        PhotoSize.Thumbnail => 400,
        PhotoSize.Card => 800,
        PhotoSize.Large => 1600,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    public static string BlobName(string originalBlobName, PhotoSize size)
    {
        var extension = Path.GetExtension(originalBlobName);
        var stem = originalBlobName[..^extension.Length];
        return $"{stem}_{LongestSide(size)}.jpg";
    }

    // The original and every copy — what has to go when the photo does. Copies are listed even if
    // they were never made: deleting a missing file is a no-op.
    public static IEnumerable<string> AllBlobNames(Photo photo) =>
        [photo.BlobName, .. All.Select(size => BlobName(photo.BlobName, size))];

    // The original until the copies exist.
    public static string Url(Photo photo, PhotoSize size, IBlobStorageService blobStorageService) =>
        blobStorageService.GetPublicUrl(photo.SizesVersion > 0 ? BlobName(photo.BlobName, size) : photo.BlobName);
}
