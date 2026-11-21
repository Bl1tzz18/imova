using Imova.Application.Features.Media.ConfirmMediaUpload;

namespace Imova.UnitTests.Media;

public class ConfirmMediaUploadValidatorTests
{
    private readonly ConfirmMediaUploadValidator _validator = new();

    [Fact]
    public void Validate_WithBlobNameScopedUnderListingId_HasNoErrors()
    {
        var listingId = Guid.NewGuid();

        var result = _validator.Validate(new ConfirmMediaUploadCommand(listingId, $"{listingId}/{Guid.NewGuid()}.jpg", Guid.NewGuid(), false));

        Assert.True(result.IsValid);
    }

    // Only the exact shape RequestUploadUrl hands out: a prefix check alone would let a path step out
    // of the listing (confirm may delete a refused upload).
    [Theory]
    [InlineData("{0}/photo.jpg")]
    [InlineData("{0}/../{1}/{1}.jpg")]
    [InlineData("{0}/{1}/{1}.jpg")]
    [InlineData("{0}/{1}.jpg/")]
    [InlineData("{0}/{1}")]
    [InlineData("{0}//{1}.jpg")]
    public void Validate_WithANameRequestUploadUrlNeverHandsOut_HasError(string shape)
    {
        var listingId = Guid.NewGuid();
        var blobName = string.Format(shape, listingId, Guid.NewGuid());

        var result = _validator.Validate(new ConfirmMediaUploadCommand(listingId, blobName, Guid.NewGuid(), false));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmMediaUploadCommand.BlobName));
    }

    [Fact]
    public void Validate_WithEmptyListingId_HasError()
    {
        var result = _validator.Validate(new ConfirmMediaUploadCommand(Guid.Empty, "photo.jpg", Guid.NewGuid(), false));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmMediaUploadCommand.ListingId));
    }

    [Fact]
    public void Validate_WithEmptyBlobName_HasError()
    {
        var result = _validator.Validate(new ConfirmMediaUploadCommand(Guid.NewGuid(), "", Guid.NewGuid(), false));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmMediaUploadCommand.BlobName));
    }

    [Fact]
    public void Validate_WithBlobNameScopedUnderADifferentListingId_HasError()
    {
        var listingId = Guid.NewGuid();
        var otherListingId = Guid.NewGuid();

        var result = _validator.Validate(new ConfirmMediaUploadCommand(listingId, $"{otherListingId}/photo.jpg", Guid.NewGuid(), false));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmMediaUploadCommand.BlobName));
    }

    [Fact]
    public void Validate_WithBlobNameNotScopedAtAll_HasError()
    {
        var listingId = Guid.NewGuid();

        var result = _validator.Validate(new ConfirmMediaUploadCommand(listingId, "photo.jpg", Guid.NewGuid(), false));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmMediaUploadCommand.BlobName));
    }
}
