using ImageMagick;
using Imova.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Imova.Infrastructure.Storage;

// ImageMagick (Magick.NET, Apache-2.0), because it reads every format the upload accepts —
// HEIC/HEIF included, which most browsers can't show, so the JPEG copies are what make an iPhone
// or Samsung HEIC upload visible at all.
public sealed class MagickPhotoResizer(ILogger<MagickPhotoResizer> logger) : IPhotoResizer
{
    // Visually indistinguishable from the original at these sizes, about a tenth of the bytes.
    private const uint JpegQuality = 82;

    // Decoding a 48 MP phone photo takes a few hundred MB for a moment; two at a time keeps a small
    // container (API or Worker) well inside its memory however many photos arrive at once.
    private static readonly SemaphoreSlim Gate = new(2);

    static MagickPhotoResizer()
    {
        // A crafted "image" claiming huge dimensions is refused instead of allocated.
        ResourceLimits.Width = 20_000;
        ResourceLimits.Height = 20_000;
    }

    public async Task<IReadOnlyList<byte[]>?> ResizeToJpegAsync(
        Stream source, IReadOnlyList<int> longestSides, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        await Gate.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() => Resize(bytes, longestSides), cancellationToken);
        }
        finally
        {
            Gate.Release();
        }
    }

    private byte[][]? Resize(byte[] source, IReadOnlyList<int> longestSides)
    {
        var settings = new MagickReadSettings
        {
            // An animated GIF: its first frame.
            FrameIndex = 0,
            FrameCount = 1,
        };

        // libjpeg can decode straight to 1/2, 1/4 or 1/8 of the size, far less memory and time than
        // decoding a 48 MP phone photo in full. The hint is twice the largest copy (ImageMagick's
        // own advice: it may pick a scale a little under the hint, and the resize after it needs
        // the extra pixels to stay sharp), and only for a photo bigger than that — for a smaller
        // one libjpeg would scale *up*. Reading the header (MagickImageInfo) decodes no pixels.
        if (TryReadSize(source) is { } size)
        {
            var scale = 2.0 * longestSides.Max() / Math.Max(size.Width, size.Height);
            if (scale < 1)
            {
                settings.SetDefine(MagickFormat.Jpeg, "size", $"{Math.Ceiling(size.Width * scale)}x{Math.Ceiling(size.Height * scale)}");
            }
        }

        MagickImage image;
        try
        {
            image = new MagickImage(source, settings);
        }
        catch (MagickException ex)
        {
            logger.LogInformation(ex, "An uploaded photo could not be read as an image.");
            return null;
        }

        using (image)
        {
            // Phones store the picture sideways plus a "rotate me" flag; the copies are upright
            // pixels, since Strip below removes the flag.
            image.AutoOrient();

            // Wide-gamut photos (an iPhone's Display P3) would look washed out once their colour
            // profile is stripped, so they're converted to the web's sRGB first.
            if (image.GetColorProfile() is not null)
            {
                image.TransformColorSpace(ColorProfiles.SRGB);
            }
            else if (image.ColorSpace != ColorSpace.sRGB)
            {
                image.ColorSpace = ColorSpace.sRGB;
            }

            // Camera details and GPS coordinates don't belong in a public file.
            image.Strip();

            // JPEG has no transparency: a transparent PNG goes on white rather than black.
            if (image.HasAlpha)
            {
                image.BackgroundColor = MagickColors.White;
                image.Alpha(AlphaOption.Remove);
            }

            image.Quality = JpegQuality;
            image.Settings.Interlace = Interlace.Jpeg;

            return longestSides
                .Select(side =>
                {
                    using var copy = image.Clone();

                    // Greater: only ever shrinks.
                    copy.Resize(new MagickGeometry((uint)side, (uint)side) { Greater = true });
                    return copy.ToByteArray(MagickFormat.Jpeg);
                })
                .ToArray();
        }
    }

    private static (uint Width, uint Height)? TryReadSize(byte[] source)
    {
        try
        {
            var info = new MagickImageInfo(source);
            return (info.Width, info.Height);
        }
        catch (MagickException)
        {
            return null;
        }
    }
}
