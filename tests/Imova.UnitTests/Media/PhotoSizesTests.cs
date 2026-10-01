using System.Text;
using Imova.Application.Features.Listings;
using Imova.Application.Features.Media.Sizes;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Media;

public class PhotoSizesTests
{
    [Theory]
    [InlineData("L/abc.png", PhotoSize.Thumbnail, "L/abc_400.jpg")]
    [InlineData("L/abc.jpeg", PhotoSize.Card, "L/abc_800.jpg")]
    [InlineData("L/abc.heic", PhotoSize.Large, "L/abc_1600.jpg")]
    [InlineData("L/abc", PhotoSize.Card, "L/abc_800.jpg")]
    public void BlobName_SitsNextToTheOriginal(string original, PhotoSize size, string expected) =>
        Assert.Equal(expected, PhotoSizes.BlobName(original, size));

    [Fact]
    public void AllBlobNames_AreTheOriginalAndEverySize()
    {
        var photo = Photo.Create(Guid.NewGuid(), "L/abc.png", "image/png", 100);

        Assert.Equal(["L/abc.png", "L/abc_400.jpg", "L/abc_800.jpg", "L/abc_1600.jpg"], PhotoSizes.AllBlobNames(photo));
    }

    [Fact]
    public void Dto_ShowsTheOriginalUntilTheSizesExist()
    {
        var blobs = new FakeBlobStorageService();
        var photo = Photo.Create(Guid.NewGuid(), "L/abc.png", "image/png", 100);

        var before = photo.ToDto(blobs);
        photo.MarkSizesGenerated(PhotoSizes.CurrentVersion);
        var after = photo.ToDto(blobs);

        Assert.All([before.Url, before.CardUrl, before.ThumbnailUrl], url => Assert.Equal("https://blob.test/L/abc.png", url));
        Assert.Equal("https://blob.test/L/abc_1600.jpg", after.Url);
        Assert.Equal("https://blob.test/L/abc_800.jpg", after.CardUrl);
        Assert.Equal("https://blob.test/L/abc_400.jpg", after.ThumbnailUrl);
    }

    [Fact]
    public void MarkSizesGenerated_ClearsAnEarlierFailure_AndRefusesVersionZero()
    {
        var photo = Photo.Create(Guid.NewGuid(), "L/abc.png", "image/png", 100);
        photo.MarkSizesFailed(DateTimeOffset.UtcNow);

        photo.MarkSizesGenerated(1);

        Assert.Equal(1, photo.SizesVersion);
        Assert.Null(photo.SizesFailedAt);
        Assert.Throws<ArgumentOutOfRangeException>(() => photo.MarkSizesGenerated(0));
    }

    [Fact]
    public async Task Generator_WritesOneJpegPerSize_FromTheOriginal()
    {
        var blobs = new FakeBlobStorageService();
        blobs.StoredBlobNames.Add("L/abc.png");
        var photo = Photo.Create(Guid.NewGuid(), "L/abc.png", "image/png", 100);

        var outcome = await new PhotoSizeGenerator(blobs, new FakePhotoResizer()).GenerateAsync(photo, CancellationToken.None);

        Assert.Equal(PhotoSizeOutcome.Generated, outcome);
        Assert.Equal(
            ["jpeg-400", "jpeg-800", "jpeg-1600"],
            new[] { "L/abc_400.jpg", "L/abc_800.jpg", "L/abc_1600.jpg" }.Select(n => Encoding.UTF8.GetString(blobs.UploadedContent[n])));
        Assert.Equal(PhotoSizes.CurrentVersion, photo.SizesVersion);
    }

    [Fact]
    public async Task Generator_WithoutTheOriginal_ChangesNothing()
    {
        var blobs = new FakeBlobStorageService();
        var resizer = new FakePhotoResizer();
        var photo = Photo.Create(Guid.NewGuid(), "L/gone.png", "image/png", 100);

        var outcome = await new PhotoSizeGenerator(blobs, resizer).GenerateAsync(photo, CancellationToken.None);

        Assert.Equal(PhotoSizeOutcome.SourceMissing, outcome);
        Assert.Equal(0, resizer.Calls);
        Assert.Empty(blobs.UploadedContent);
        Assert.Equal(0, photo.SizesVersion);
    }
}

public class PhotoSizeBackfillTests
{
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly FakeBlobStorageService _blobs = new();
    private readonly FakePhotoResizer _resizer = new();
    private readonly ManualTimeProvider _clock = new(DateTimeOffset.UtcNow);

    private Photo AddPhoto(string name, bool stored = true)
    {
        var photo = Photo.Create(Guid.NewGuid(), name, "image/jpeg", 100);
        _db.Photos.Add(photo);
        _db.SaveChanges();
        if (stored)
        {
            _blobs.StoredBlobNames.Add(name);
        }

        return photo;
    }

    private Task<int> RunAsync() => new PhotoSizeBackfill(
            _db, new PhotoSizeGenerator(_blobs, _resizer), _clock, NullLogger<PhotoSizeBackfill>.Instance)
        .RunAsync(CancellationToken.None);

    [Fact]
    public async Task PhotosWithoutSizes_GetThem_AndTheNextRunHasNothingToDo()
    {
        var first = AddPhoto("L/one.jpg");
        var second = AddPhoto("L/two.jpg");

        Assert.Equal(2, await RunAsync());
        Assert.Equal(PhotoSizes.CurrentVersion, first.SizesVersion);
        Assert.Equal(PhotoSizes.CurrentVersion, second.SizesVersion);
        Assert.Contains("L/two_800.jpg", _blobs.UploadedContent.Keys);

        Assert.Equal(0, await RunAsync());
        Assert.Equal(2, _resizer.Calls);
    }

    [Fact]
    public async Task APhotoWhoseOriginalIsGoneOrUnreadable_IsMarkedAndNotTriedAgain()
    {
        var gone = AddPhoto("L/gone.jpg", stored: false);

        Assert.Equal(0, await RunAsync());
        Assert.Equal(_clock.GetUtcNow(), gone.SizesFailedAt);

        _resizer.Unreadable = true;
        var unreadable = AddPhoto("L/broken.jpg");
        Assert.Equal(0, await RunAsync());
        Assert.NotNull(unreadable.SizesFailedAt);

        Assert.Equal(0, await RunAsync());
        Assert.Equal(1, _resizer.Calls);
        Assert.Equal(0, gone.SizesVersion);
    }

    [Fact]
    public async Task APassingFailure_IsRetriedNextRun()
    {
        var photo = AddPhoto("L/later.jpg");
        _resizer.Throws = true;

        Assert.Equal(0, await RunAsync());
        Assert.Null(photo.SizesFailedAt);

        _resizer.Throws = false;
        Assert.Equal(1, await RunAsync());
        Assert.Equal(PhotoSizes.CurrentVersion, photo.SizesVersion);
    }
}
