using System.Text;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Messaging;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Messaging;

public class MessageImageCopiesTests
{
    private const string Original = "messages/2f0c6a52-9d0e-4bd5-8b0e-0d4c3c1b8a11/7d1f0e0a-1111-4c4c-9a9a-123456789abc.png";

    private static (MessageImageCopies Copies, FakeBlobStorageService Blobs, FakePhotoResizer Resizer) Create()
    {
        var blobs = new FakeBlobStorageService();
        blobs.MessageAttachmentInfoByName[Original] = new UploadedBlobInfo(1024, "image/png", []);
        var resizer = new FakePhotoResizer();
        return (new MessageImageCopies(blobs, resizer, NullLogger<MessageImageCopies>.Instance), blobs, resizer);
    }

    private static string Read(MessageImageContent image)
    {
        using var reader = new StreamReader(image.Content, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    [Fact]
    public void Copies_SitNextToTheOriginal_AsJpegs()
    {
        Assert.EndsWith("7d1f0e0a-1111-4c4c-9a9a-123456789abc_800.jpg", MessageImageSizes.BlobName(Original, MessageImageSize.Thumbnail));
        Assert.EndsWith("7d1f0e0a-1111-4c4c-9a9a-123456789abc_1600.jpg", MessageImageSizes.BlobName(Original, MessageImageSize.Large));
        Assert.Equal(Original, MessageImageSizes.BlobName(Original, MessageImageSize.Original));
        Assert.Equal(3, MessageImageSizes.AllBlobNames(Original).Distinct().Count());
    }

    [Theory]
    [InlineData(null, MessageImageSize.Original)]
    [InlineData("", MessageImageSize.Original)]
    [InlineData("thumbnail", MessageImageSize.Thumbnail)]
    [InlineData("LARGE", MessageImageSize.Large)]
    public void TryParse_ReadsTheSizeParameter(string? value, MessageImageSize expected)
    {
        Assert.True(MessageImageSizes.TryParse(value, out var size));
        Assert.Equal(expected, size);
    }

    [Fact]
    public void TryParse_RefusesAnythingElse()
    {
        Assert.False(MessageImageSizes.TryParse("huge", out _));
    }

    [Fact]
    public async Task AFirstRequest_MakesBothCopiesFromOneDecode_StoresThem_AndServesTheOneAskedFor()
    {
        var (copies, blobs, resizer) = Create();

        var image = await copies.OpenAsync(Original, "image/png", MessageImageSize.Thumbnail, CancellationToken.None);

        Assert.NotNull(image);
        Assert.Equal("image/jpeg", image.ContentType);
        Assert.Equal("jpeg-800", Read(image));
        Assert.Equal(1, resizer.Calls);
        Assert.Equal("jpeg-800"u8.ToArray(), blobs.MessageAttachmentContent[MessageImageSizes.BlobName(Original, MessageImageSize.Thumbnail)]);
        Assert.Equal("jpeg-1600"u8.ToArray(), blobs.MessageAttachmentContent[MessageImageSizes.BlobName(Original, MessageImageSize.Large)]);
    }

    [Fact]
    public async Task ALaterRequest_ServesTheStoredCopy_WithoutResizingAgain()
    {
        var (copies, _, resizer) = Create();
        await copies.OpenAsync(Original, "image/png", MessageImageSize.Thumbnail, CancellationToken.None);

        var image = await copies.OpenAsync(Original, "image/png", MessageImageSize.Large, CancellationToken.None);

        Assert.Equal("jpeg-1600", Read(image!));
        Assert.Equal(1, resizer.Calls);
    }

    [Fact]
    public async Task TheOriginal_IsServedAsUploaded()
    {
        var (copies, _, resizer) = Create();

        var image = await copies.OpenAsync(Original, "image/png", MessageImageSize.Original, CancellationToken.None);

        Assert.Equal("image/png", image!.ContentType);
        Assert.Equal(0, resizer.Calls);
    }

    [Fact]
    public async Task AnImageThatCantBeResized_IsServedAsTheOriginal()
    {
        var (copies, blobs, resizer) = Create();
        resizer.Unreadable = true;

        var image = await copies.OpenAsync(Original, "image/png", MessageImageSize.Thumbnail, CancellationToken.None);

        Assert.Equal("image/png", image!.ContentType);
        Assert.Empty(blobs.MessageAttachmentContent);
    }

    [Fact]
    public async Task AResizeThatFails_ServesTheOriginal()
    {
        var (copies, _, resizer) = Create();
        resizer.Throws = true;

        var image = await copies.OpenAsync(Original, "image/png", MessageImageSize.Large, CancellationToken.None);

        Assert.Equal("image/png", image!.ContentType);
    }

    [Fact]
    public async Task ACopyThatCantBeStored_IsStillServed()
    {
        var (copies, blobs, _) = Create();
        blobs.FailMessageAttachmentUploads = true;

        var image = await copies.OpenAsync(Original, "image/png", MessageImageSize.Thumbnail, CancellationToken.None);

        Assert.Equal("jpeg-800", Read(image!));
    }

    [Fact]
    public async Task AMissingOriginal_IsNull()
    {
        var (copies, _, _) = Create();

        Assert.Null(await copies.OpenAsync("messages/x/gone.png", "image/png", MessageImageSize.Thumbnail, CancellationToken.None));
        Assert.Null(await copies.OpenAsync("messages/x/gone.png", "image/png", MessageImageSize.Original, CancellationToken.None));
    }
}
