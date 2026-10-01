using ImageMagick;
using Imova.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.IntegrationTests.Media;

// The real ImageMagick resizer (no stack needed) — what the display sizes of a photo look like.
public class MagickPhotoResizerTests
{
    private static readonly MagickPhotoResizer Resizer = new(NullLogger<MagickPhotoResizer>.Instance);

    private static async Task<List<MagickImage>> ResizeAsync(byte[] source, params int[] sides)
    {
        var result = await Resizer.ResizeToJpegAsync(new MemoryStream(source), sides, CancellationToken.None);
        Assert.NotNull(result);
        return result.Select(bytes => new MagickImage(bytes)).ToList();
    }

    [Fact]
    public async Task ShrinksToEachLongestSide_AsJpeg_KeepingTheAspectRatio()
    {
        var source = new MagickImage(MagickColors.Teal, 3000, 2000).ToByteArray(MagickFormat.Jpeg);

        var copies = await ResizeAsync(source, 400, 800, 1600);

        Assert.All(copies, c => Assert.Equal(MagickFormat.Jpeg, c.Format));
        Assert.Equal([(400u, 267u), (800u, 533u), (1600u, 1067u)], copies.Select(c => (c.Width, c.Height)));
    }

    [Fact]
    public async Task APhoneSizedPhoto_DecodedSmallerForSpeed_StillGetsFullSizeCopies()
    {
        // 8000 px: big enough for the reduced-size JPEG decode to kick in.
        var source = new MagickImage(MagickColors.Teal, 8000, 6000).ToByteArray(MagickFormat.Jpeg);

        var copies = await ResizeAsync(source, 400, 800, 1600);

        Assert.Equal([(400u, 300u), (800u, 600u), (1600u, 1200u)], copies.Select(c => (c.Width, c.Height)));
    }

    [Fact]
    public async Task NeverEnlargesASmallPhoto()
    {
        var source = new MagickImage(MagickColors.Teal, 300, 200).ToByteArray(MagickFormat.Png);

        var copies = await ResizeAsync(source, 400, 1600);

        Assert.All(copies, c => Assert.Equal((300u, 200u), (c.Width, c.Height)));
    }

    [Fact]
    public async Task TurnsAPhoneShotUpright_AndDropsCameraDetails()
    {
        // Stored landscape with "rotate 90° clockwise to view" (EXIF orientation 6), as phones do.
        using var shot = new MagickImage(MagickColors.Teal, 200, 100);
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Orientation, (ushort)6);
        exif.SetValue(ExifTag.Make, "TestCam");
        shot.SetProfile(exif);
        shot.Orientation = OrientationType.RightTop;

        var copy = Assert.Single(await ResizeAsync(shot.ToByteArray(MagickFormat.Jpeg), 400));

        Assert.Equal((100u, 200u), (copy.Width, copy.Height));
        Assert.Null(copy.GetExifProfile());
    }

    [Fact]
    public async Task PutsATransparentPngOnWhite()
    {
        var source = new MagickImage(MagickColors.Transparent, 50, 50).ToByteArray(MagickFormat.Png);

        var copy = Assert.Single(await ResizeAsync(source, 400));

        using var pixels = copy.GetPixels();
        var color = pixels.GetPixel(25, 25).ToColor()!;
        Assert.True(color.R > 250 && color.G > 250 && color.B > 250, $"Expected white, got {color}.");
    }

    // A real 64x48 HEIC (made with libheif/x265 via pillow-heif): the ImageMagick bundled here can
    // read HEIC but not write it, so the test can't make one itself.
    private static readonly byte[] Heic = Convert.FromBase64String(
        "AAAAHGZ0eXBoZWljAAAAAG1pZjFoZWljbWlhZgAAAX1tZXRhAAAAAAAAACFoZGxyAAAAAAAAAABwaWN0AAAAAAAAAAAAAAAAAAAA" +
        "ACJpbG9jAAAAAERAAAEAAQAAAAABoQABAAAAAAAAAB0AAAAjaWluZgAAAAAAAQAAABVpbmZlAgAAAAABAABodmMxAAAAAA5waXRt" +
        "AAAAAAABAAAA/WlwcnAAAADdaXBjbwAAAHZodmNDAQNwAAAAAAAAAAAAHvAA/P34+AAADwNgAAEAGEABDAH//wNwAAADAJAAAAMA" +
        "AAMAHroCQGEAAQAqQgEBA3AAAAMAkAAAAwAAAwAeoCCBBZbq5Ka5uAhoMCAAAAMDIAAAAwAhYgABAAZEAcFzwIkAAAATY29scm5j" +
        "bHgAAQANAAaAAAAAFGlzcGUAAAAAAAAAQAAAAEAAAAAoY2xhcAAAAEAAAAABAAAAMAAAAAEAAAAAAAAAAv////AAAAACAAAAEHBp" +
        "eGkAAAAAAwgICAAAABhpcG1hAAAAAAAAAAEAAQWBAgMFhAAAACVtZGF0AAAAGSgBrxOA+BDhp//6uKK/1K1f+n+S/OikM+A=");

    [Fact]
    public async Task ReadsHeic_AsUploadedFromPhones()
    {
        var copy = Assert.Single(await ResizeAsync(Heic, 400));

        Assert.Equal(MagickFormat.Jpeg, copy.Format);
        Assert.Equal((64u, 48u), (copy.Width, copy.Height));
    }

    [Fact]
    public async Task SomethingThatIsNotAnImage_GivesNull()
    {
        byte[] fake = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52];

        Assert.Null(await Resizer.ResizeToJpegAsync(new MemoryStream(fake), [400], CancellationToken.None));
    }
}
