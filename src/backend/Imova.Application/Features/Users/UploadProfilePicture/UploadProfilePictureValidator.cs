using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Validation;

namespace Imova.Application.Features.Users.UploadProfilePicture;

public class UploadProfilePictureValidator : AbstractValidator<UploadProfilePictureCommand>
{
    // Deliberately tighter than Photo.MaxFileSizeBytes (10MB) — a single profile picture
    // has no business being that large.
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public UploadProfilePictureValidator()
    {
        RuleFor(c => c.Content).NotEmpty();
        RuleFor(c => c.Content)
            .Must(content => content.Length <= MaxFileSizeBytes)
            .WithMessage($"File exceeds the {MaxFileSizeBytes / (1024 * 1024)}MB limit.").WithErrorCode(ErrorCodes.UploadTooLarge).WithState(_ => CodedFailure.Params(("maxMb", MaxFileSizeBytes / (1024 * 1024))))
            .When(c => c.Content.Length > 0);
    }
}
