using Imova.Application.Features.Media.ConfirmMediaUpload;

namespace Imova.UnitTests.Media;

public class ConfirmMediaUploadValidatorTests
{
    private readonly ConfirmMediaUploadValidator _validator = new();

    [Fact]
    public void Validate_WithBlobNameScopedUnderListingId_HasNoErrors()
    {
        var listingId = Guid.NewGuid();

        var result = _validator.Validate(new ConfirmMediaUploadCommand(listingId, $"{listingId}/photo.jpg"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyListingId_HasError()
    {
        var result = _validator.Validate(new ConfirmMediaUploadCommand(Guid.Empty, "photo.jpg"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmMediaUploadCommand.ListingId));
    }

    [Fact]
    public void Validate_WithEmptyBlobName_HasError()
    {
        var result = _validator.Validate(new ConfirmMediaUploadCommand(Guid.NewGuid(), ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmMediaUploadCommand.BlobName));
    }

    [Fact]
    public void Validate_WithBlobNameScopedUnderADifferentListingId_HasError()
    {
        var listingId = Guid.NewGuid();
        var otherListingId = Guid.NewGuid();

        var result = _validator.Validate(new ConfirmMediaUploadCommand(listingId, $"{otherListingId}/photo.jpg"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmMediaUploadCommand.BlobName));
    }

    [Fact]
    public void Validate_WithBlobNameNotScopedAtAll_HasError()
    {
        var listingId = Guid.NewGuid();

        var result = _validator.Validate(new ConfirmMediaUploadCommand(listingId, "photo.jpg"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmMediaUploadCommand.BlobName));
    }
}
