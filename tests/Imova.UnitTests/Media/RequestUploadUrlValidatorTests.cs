using Imova.Application.Features.Media.RequestUploadUrl;

namespace Imova.UnitTests.Media;

public class RequestUploadUrlValidatorTests
{
    private readonly RequestUploadUrlValidator _validator = new();

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    [InlineData(".webp")]
    [InlineData(".gif")]
    [InlineData(".bmp")]
    [InlineData(".heic")]
    [InlineData(".heif")]
    public void Validate_WithAllowedExtension_HasNoErrors(string extension)
    {
        var result = _validator.Validate(new RequestUploadUrlCommand(Guid.NewGuid(), extension));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyPropertyId_HasError()
    {
        var result = _validator.Validate(new RequestUploadUrlCommand(Guid.Empty, ".jpg"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RequestUploadUrlCommand.PropertyId));
    }

    [Fact]
    public void Validate_WithEmptyFileExtension_HasError()
    {
        var result = _validator.Validate(new RequestUploadUrlCommand(Guid.NewGuid(), ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RequestUploadUrlCommand.FileExtension));
    }

    [Theory]
    [InlineData(".exe")]
    [InlineData(".svg")]
    [InlineData("jpg")]
    public void Validate_WithDisallowedFileExtension_HasError(string extension)
    {
        var result = _validator.Validate(new RequestUploadUrlCommand(Guid.NewGuid(), extension));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RequestUploadUrlCommand.FileExtension));
    }
}
