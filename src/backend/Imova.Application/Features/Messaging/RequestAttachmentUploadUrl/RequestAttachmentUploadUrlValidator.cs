using FluentValidation;
using Imova.Domain.Listings;

namespace Imova.Application.Features.Messaging.RequestAttachmentUploadUrl;

public class RequestAttachmentUploadUrlValidator : AbstractValidator<RequestAttachmentUploadUrlCommand>
{
    public RequestAttachmentUploadUrlValidator()
    {
        RuleFor(c => c.FileExtension)
            .NotEmpty()
            .Must(ext => Photo.AllowedContentTypesByExtension.ContainsKey(ext))
            .WithMessage($"FileExtension must be one of: {string.Join(", ", Photo.AllowedContentTypesByExtension.Keys)}.");
    }
}
