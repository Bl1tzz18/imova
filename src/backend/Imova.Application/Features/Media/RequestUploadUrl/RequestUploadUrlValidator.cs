using FluentValidation;
using Imova.Domain.Media;

namespace Imova.Application.Features.Media.RequestUploadUrl;

public class RequestUploadUrlValidator : AbstractValidator<RequestUploadUrlCommand>
{
    public RequestUploadUrlValidator()
    {
        RuleFor(c => c.PropertyId).NotEmpty();

        RuleFor(c => c.FileExtension)
            .NotEmpty()
            .Must(ext => PropertyMedia.AllowedContentTypesByExtension.ContainsKey(ext))
            .WithMessage($"FileExtension must be one of: {string.Join(", ", PropertyMedia.AllowedContentTypesByExtension.Keys)}.");
    }
}
