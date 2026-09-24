using FluentValidation;
using Imova.Domain.Listings;

namespace Imova.Application.Features.Media.RequestUploadUrl;

public class RequestUploadUrlValidator : AbstractValidator<RequestUploadUrlCommand>
{
    public RequestUploadUrlValidator()
    {
        RuleFor(c => c.ListingId).NotEmpty();

        RuleFor(c => c.FileExtension)
            .NotEmpty()
            .Must(ext => Photo.AllowedContentTypesByExtension.ContainsKey(ext))
            .WithMessage($"FileExtension must be one of: {string.Join(", ", Photo.AllowedContentTypesByExtension.Keys)}.");
    }
}
