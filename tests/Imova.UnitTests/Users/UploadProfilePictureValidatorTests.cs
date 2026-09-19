using Imova.Application.Features.Users.UploadProfilePicture;

namespace Imova.UnitTests.Users;

public class UploadProfilePictureValidatorTests
{
    private readonly UploadProfilePictureValidator _validator = new();

    [Fact]
    public void Validate_WithNonEmptyContentUnderLimit_HasNoErrors()
    {
        var result = _validator.Validate(new UploadProfilePictureCommand(Guid.NewGuid(), [1, 2, 3]));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyContent_HasError()
    {
        var result = _validator.Validate(new UploadProfilePictureCommand(Guid.NewGuid(), []));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UploadProfilePictureCommand.Content));
    }

    [Fact]
    public void Validate_WithContentOverTheLimit_HasError()
    {
        var oversized = new byte[UploadProfilePictureValidator.MaxFileSizeBytes + 1];

        var result = _validator.Validate(new UploadProfilePictureCommand(Guid.NewGuid(), oversized));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UploadProfilePictureCommand.Content));
    }

    [Fact]
    public void Validate_WithContentExactlyAtTheLimit_HasNoErrors()
    {
        var atLimit = new byte[UploadProfilePictureValidator.MaxFileSizeBytes];

        var result = _validator.Validate(new UploadProfilePictureCommand(Guid.NewGuid(), atLimit));

        Assert.True(result.IsValid);
    }
}
