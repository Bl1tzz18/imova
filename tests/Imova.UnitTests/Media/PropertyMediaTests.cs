using Imova.Domain.Media;

namespace Imova.UnitTests.Media;

public class PropertyMediaTests
{
    private static readonly Guid PropertyId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_SetsAllFieldsAndStartsPending()
    {
        var media = PropertyMedia.Create(PropertyId, "abc/photo.jpg", "image/jpeg", 1024, sortOrder: 2);

        Assert.NotEqual(Guid.Empty, media.Id);
        Assert.Equal(PropertyId, media.PropertyId);
        Assert.Equal("abc/photo.jpg", media.BlobName);
        Assert.Equal("image/jpeg", media.ContentType);
        Assert.Equal(1024, media.FileSizeBytes);
        Assert.Equal(2, media.SortOrder);
        Assert.Equal(ModerationStatus.Pending, media.ModerationStatus);
        Assert.Null(media.ModeratedAt);
    }

    [Fact]
    public void Create_WithDefaultSortOrder_IsZero()
    {
        var media = PropertyMedia.Create(PropertyId, "abc/photo.jpg", "image/jpeg", 1024);

        Assert.Equal(0, media.SortOrder);
    }

    [Fact]
    public void Create_WithEmptyPropertyId_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => PropertyMedia.Create(Guid.Empty, "abc/photo.jpg", "image/jpeg", 1024));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithMissingBlobName_Throws(string blobName)
    {
        Assert.ThrowsAny<ArgumentException>(() => PropertyMedia.Create(PropertyId, blobName, "image/jpeg", 1024));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveFileSize_Throws(long fileSizeBytes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PropertyMedia.Create(PropertyId, "abc/photo.jpg", "image/jpeg", fileSizeBytes));
    }

    [Fact]
    public void Create_WithFileSizeOverLimit_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PropertyMedia.Create(PropertyId, "abc/photo.jpg", "image/jpeg", PropertyMedia.MaxFileSizeBytes + 1));
    }

    [Fact]
    public void Create_WithFileSizeAtLimit_Succeeds()
    {
        var media = PropertyMedia.Create(PropertyId, "abc/photo.jpg", "image/jpeg", PropertyMedia.MaxFileSizeBytes);

        Assert.Equal(PropertyMedia.MaxFileSizeBytes, media.FileSizeBytes);
    }

    [Fact]
    public void Approve_SetsApprovedStatusAndModeratedAt()
    {
        var media = PropertyMedia.Create(PropertyId, "abc/photo.jpg", "image/jpeg", 1024);

        media.Approve();

        Assert.Equal(ModerationStatus.Approved, media.ModerationStatus);
        Assert.NotNull(media.ModeratedAt);
    }

    [Fact]
    public void Reject_SetsRejectedStatusAndModeratedAt()
    {
        var media = PropertyMedia.Create(PropertyId, "abc/photo.jpg", "image/jpeg", 1024);

        media.Reject();

        Assert.Equal(ModerationStatus.Rejected, media.ModerationStatus);
        Assert.NotNull(media.ModeratedAt);
    }
}
