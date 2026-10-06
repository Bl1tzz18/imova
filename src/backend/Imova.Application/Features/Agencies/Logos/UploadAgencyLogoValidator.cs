using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Validation;

namespace Imova.Application.Features.Agencies.Logos;

public class UploadAgencyLogoValidator : AbstractValidator<UploadAgencyLogoCommand>
{
    public UploadAgencyLogoValidator()
    {
        RuleFor(c => c.Content).NotEmpty();
        RuleFor(c => c.Content)
            .Must(content => content.Length <= AgencyLogo.MaxFileSizeBytes)
            .WithMessage($"File exceeds the {AgencyLogo.MaxFileSizeBytes / (1024 * 1024)}MB limit.")
            .WithErrorCode(ErrorCodes.UploadTooLarge)
            .WithState(_ => CodedFailure.Params(("maxMb", AgencyLogo.MaxFileSizeBytes / (1024 * 1024))))
            .When(c => c.Content.Length > 0);
    }
}
