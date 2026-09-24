using Imova.Domain.Listings;

namespace Imova.UnitTests.Listings;

public class PhotoTests
{
    private static readonly Guid ListingId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_SetsAllFieldsAndStartsPending()
    {
        var photo = Photo.Create(ListingId, "abc/photo.jpg", "image/jpeg", 1024, sortOrder: 2);

        Assert.NotEqual(Guid.Empty, photo.Id);
        Assert.Equal(ListingId, photo.ListingId);
        Assert.Equal("abc/photo.jpg", photo.BlobName);
        Assert.Equal("image/jpeg", photo.ContentType);
        Assert.Equal(1024, photo.FileSizeBytes);
        Assert.Equal(2, photo.SortOrder);
        Assert.False(photo.IsPrimary);
        Assert.Equal(ModerationStatus.Pending, photo.ModerationStatus);
        Assert.Null(photo.ModeratedAt);
    }

    [Fact]
    public void Create_WithDefaultSortOrder_IsZero()
    {
        var photo = Photo.Create(ListingId, "abc/photo.jpg", "image/jpeg", 1024);

        Assert.Equal(0, photo.SortOrder);
    }

    [Fact]
    public void Create_WithEmptyListingId_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => Photo.Create(Guid.Empty, "abc/photo.jpg", "image/jpeg", 1024));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithMissingBlobName_Throws(string blobName)
    {
        Assert.ThrowsAny<ArgumentException>(() => Photo.Create(ListingId, blobName, "image/jpeg", 1024));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveFileSize_Throws(long fileSizeBytes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Photo.Create(ListingId, "abc/photo.jpg", "image/jpeg", fileSizeBytes));
    }

    [Fact]
    public void Create_WithFileSizeOverLimit_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Photo.Create(ListingId, "abc/photo.jpg", "image/jpeg", Photo.MaxFileSizeBytes + 1));
    }

    [Fact]
    public void Create_WithFileSizeAtLimit_Succeeds()
    {
        var photo = Photo.Create(ListingId, "abc/photo.jpg", "image/jpeg", Photo.MaxFileSizeBytes);

        Assert.Equal(Photo.MaxFileSizeBytes, photo.FileSizeBytes);
    }

    [Fact]
    public void Approve_SetsApprovedStatusAndModeratedAt()
    {
        var photo = Photo.Create(ListingId, "abc/photo.jpg", "image/jpeg", 1024);

        photo.Approve();

        Assert.Equal(ModerationStatus.Approved, photo.ModerationStatus);
        Assert.NotNull(photo.ModeratedAt);
    }

    [Fact]
    public void Reject_SetsRejectedStatusAndModeratedAt()
    {
        var photo = Photo.Create(ListingId, "abc/photo.jpg", "image/jpeg", 1024);

        photo.Reject();

        Assert.Equal(ModerationStatus.Rejected, photo.ModerationStatus);
        Assert.NotNull(photo.ModeratedAt);
    }

    [Fact]
    public void MarkAsPrimary_SetsIsPrimary()
    {
        var photo = Photo.Create(ListingId, "abc/photo.jpg", "image/jpeg", 1024);

        photo.MarkAsPrimary();

        Assert.True(photo.IsPrimary);
    }

    [Fact]
    public void Create_WithIsPrimary_SetsIsPrimary()
    {
        var photo = Photo.Create(ListingId, "abc/photo.jpg", "image/jpeg", 1024, isPrimary: true);

        Assert.True(photo.IsPrimary);
    }
}
