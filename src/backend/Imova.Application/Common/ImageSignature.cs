using System.Text;
using Imova.Domain.Media;

namespace Imova.Application.Common;

// Sniffs the actual file type from its leading bytes ("magic numbers") instead of trusting the
// client-reported content-type or file extension, which are easy to spoof.
public static class ImageSignature
{
    // Built from PropertyMedia's own extension->contentType map (the existing source of truth
    // for "which image formats does this app accept") rather than a second, separately
    // maintained list — reused by both the profile-picture upload and Google picture sync flows.
    private static readonly IReadOnlyDictionary<string, string> ExtensionByContentType =
        PropertyMedia.AllowedContentTypesByExtension
            .GroupBy(kvp => kvp.Value, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Key, StringComparer.OrdinalIgnoreCase);

    public static string? ExtensionForContentType(string contentType) =>
        ExtensionByContentType.GetValueOrDefault(contentType);

    // ISOBMFF (the container HEIC/HEIF use) "major brand" values — see the ftyp check below.
    private static readonly HashSet<string> HeicBrands = ["heic", "heix", "heim", "heis", "hevc", "hevx"];
    private static readonly HashSet<string> HeifBrands = ["mif1", "msf1"];

    public static string? DetectContentType(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (header.Length >= 8 &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            return "image/png";
        }

        if (header.Length >= 12 &&
            header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F' &&
            header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P')
        {
            return "image/webp";
        }

        if (header.Length >= 6 &&
            header[0] == (byte)'G' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'8' &&
            (header[4] == (byte)'7' || header[4] == (byte)'9') && header[5] == (byte)'a')
        {
            return "image/gif";
        }

        if (header.Length >= 2 && header[0] == (byte)'B' && header[1] == (byte)'M')
        {
            return "image/bmp";
        }

        // HEIC/HEIF: an ISOBMFF container — a leading box size (4 bytes, value not checked
        // here), then "ftyp", then a 4-byte major brand identifying the specific codec/profile.
        if (header.Length >= 12 &&
            header[4] == (byte)'f' && header[5] == (byte)'t' && header[6] == (byte)'y' && header[7] == (byte)'p')
        {
            var brand = Encoding.ASCII.GetString(header.Slice(8, 4));

            if (HeicBrands.Contains(brand))
            {
                return "image/heic";
            }

            if (HeifBrands.Contains(brand))
            {
                return "image/heif";
            }
        }

        return null;
    }
}
