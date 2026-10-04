using System.Globalization;
using Imova.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Agencies.Logos;

// An agency's logo is stored as two square JPEGs on white — 512 px for its page, 128 px for small
// places (cards, the listing's contact card) — under agencies/{agencyId}/. Agency.LogoBlobName is
// the 512 one; the 128 one sits next to it. Each upload gets new names (a time stamp), so a cached
// old logo never shows after a change.
public static class AgencyLogo
{
    public const int Size = 512;
    public const int ThumbnailSize = 128;

    // Smaller than this on either side and the logo would be blown up into a blur.
    public const int MinSide = 200;

    // Logos are small; anything bigger is a mistake.
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    // What a logo may be uploaded as (sniffed from the bytes — see ImageSignature).
    public static readonly IReadOnlySet<string> AllowedContentTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

    private const string LargeSuffix = "_512.jpg";
    private const string ThumbnailSuffix = "_128.jpg";

    public static string BlobName(Guid agencyId, DateTimeOffset now) =>
        $"agencies/{agencyId}/logo-{now.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)}{LargeSuffix}";

    public static string ThumbnailBlobName(string blobName) =>
        blobName.EndsWith(LargeSuffix, StringComparison.Ordinal)
            ? blobName[..^LargeSuffix.Length] + ThumbnailSuffix
            : blobName;

    // Every file of a logo, to delete.
    public static IReadOnlyList<string> AllBlobNames(string blobName) =>
        ThumbnailBlobName(blobName) is var thumbnail && thumbnail != blobName ? [blobName, thumbnail] : [blobName];

    // Best-effort: once the agency no longer points at a logo, a file that won't delete is only
    // wasted space, never a reason to fail the request.
    public static async Task DeleteFilesAsync(
        IBlobStorageService blobStorageService, string blobName, ILogger logger, CancellationToken cancellationToken)
    {
        foreach (var name in AllBlobNames(blobName))
        {
            try
            {
                await blobStorageService.DeleteAsync(name, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not delete the old agency logo file {BlobName}.", name);
            }
        }
    }
}
