using Imova.Application.Features.Media.ConfirmMediaUpload;

namespace Imova.UnitTests.Media;

public class ConfirmMediaUploadValidatorTests
{
    private readonly ConfirmMediaUploadValidator _validator = new();

    [Fact]
    public void Validate_WithBlobNameScopedUnderPropertyId_HasNoErrors()
    {
        var propertyId = Guid.NewGuid();

        var result = _validator.Validate(new ConfirmMediaUploadCommand(propertyId, $"{propertyId}/photo.jpg"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyPropertyId_HasError()
    {
        var result = _validator.Validate(new ConfirmMediaUploadCommand(Guid.Empty, "photo.jpg"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmMediaUploadCommand.PropertyId));
    }

    [Fact]
    public void Validate_WithEmptyBlobName_HasError()
    {
        var result = _validator.Validate(new ConfirmMediaUploadCommand(Guid.NewGuid(), ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmMediaUploadCommand.BlobName));
    }

    [Fact]
    public void Validate_WithBlobNameScopedUnderADifferentPropertyId_HasError()
    {
        var propertyId = Guid.NewGuid();
        var otherPropertyId = Guid.NewGuid();

        var result = _validator.Validate(new ConfirmMediaUploadCommand(propertyId, $"{otherPropertyId}/photo.jpg"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmMediaUploadCommand.BlobName));
    }

    [Fact]
    public void Validate_WithBlobNameNotScopedAtAll_HasError()
    {
        var propertyId = Guid.NewGuid();

        var result = _validator.Validate(new ConfirmMediaUploadCommand(propertyId, "photo.jpg"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConfirmMediaUploadCommand.BlobName));
    }
}
