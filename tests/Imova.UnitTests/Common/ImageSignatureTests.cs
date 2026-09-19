using Imova.Application.Common;

namespace Imova.UnitTests.Common;

public class ImageSignatureTests
{
    [Fact]
    public void DetectContentType_WithJpegSignature_ReturnsImageJpeg()
    {
        byte[] header = [0xFF, 0xD8, 0xFF, 0xE0];

        Assert.Equal("image/jpeg", ImageSignature.DetectContentType(header));
    }

    [Fact]
    public void DetectContentType_WithPngSignature_ReturnsImagePng()
    {
        byte[] header = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        Assert.Equal("image/png", ImageSignature.DetectContentType(header));
    }

    [Fact]
    public void DetectContentType_WithWebpSignature_ReturnsImageWebp()
    {
        byte[] header = [(byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P'];

        Assert.Equal("image/webp", ImageSignature.DetectContentType(header));
    }

    [Theory]
    [InlineData('7')]
    [InlineData('9')]
    public void DetectContentType_WithGifSignature_ReturnsImageGif(char version)
    {
        byte[] header = [(byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)version, (byte)'a'];

        Assert.Equal("image/gif", ImageSignature.DetectContentType(header));
    }

    [Fact]
    public void DetectContentType_WithBmpSignature_ReturnsImageBmp()
    {
        byte[] header = [(byte)'B', (byte)'M'];

        Assert.Equal("image/bmp", ImageSignature.DetectContentType(header));
    }

    [Fact]
    public void DetectContentType_WithHeicBrand_ReturnsImageHeic()
    {
        byte[] header = [0, 0, 0, 0x18, (byte)'f', (byte)'t', (byte)'y', (byte)'p', (byte)'h', (byte)'e', (byte)'i', (byte)'c'];

        Assert.Equal("image/heic", ImageSignature.DetectContentType(header));
    }

    [Fact]
    public void DetectContentType_WithHeifBrand_ReturnsImageHeif()
    {
        byte[] header = [0, 0, 0, 0x18, (byte)'f', (byte)'t', (byte)'y', (byte)'p', (byte)'m', (byte)'i', (byte)'f', (byte)'1'];

        Assert.Equal("image/heif", ImageSignature.DetectContentType(header));
    }

    [Fact]
    public void DetectContentType_WithUnrecognizedBytes_ReturnsNull()
    {
        byte[] header = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07];

        Assert.Null(ImageSignature.DetectContentType(header));
    }

    [Fact]
    public void DetectContentType_WithEmptyHeader_ReturnsNull()
    {
        Assert.Null(ImageSignature.DetectContentType(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void DetectContentType_WithTruncatedPngSignature_ReturnsNull()
    {
        // Only the first 4 of PNG's 8 magic bytes — must not false-positive on a partial match.
        byte[] header = [0x89, 0x50, 0x4E, 0x47];

        Assert.Null(ImageSignature.DetectContentType(header));
    }

    [Theory]
    [InlineData("image/png", ".png")]
    [InlineData("image/webp", ".webp")]
    [InlineData("image/gif", ".gif")]
    [InlineData("image/bmp", ".bmp")]
    [InlineData("image/heic", ".heic")]
    [InlineData("image/heif", ".heif")]
    public void ExtensionForContentType_WithKnownContentType_ReturnsExpectedExtension(string contentType, string expectedExtension)
    {
        Assert.Equal(expectedExtension, ImageSignature.ExtensionForContentType(contentType));
    }

    [Fact]
    public void ExtensionForContentType_WithUnknownContentType_ReturnsNull()
    {
        Assert.Null(ImageSignature.ExtensionForContentType("application/pdf"));
    }
}
