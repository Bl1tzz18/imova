using FluentValidation;

namespace Imova.Application.Features.Media.ConfirmMediaUpload;

public class ConfirmMediaUploadValidator : AbstractValidator<ConfirmMediaUploadCommand>
{
    public ConfirmMediaUploadValidator()
    {
        RuleFor(c => c.ListingId).NotEmpty();
        RuleFor(c => c.BlobName).NotEmpty();

        // Cheap defense-in-depth: makes sure a client can only confirm a blob scoped under the
        // listing it claims, before we even ask storage whether the blob exists.
        RuleFor(c => c.BlobName)
            .Must((command, blobName) => blobName.StartsWith($"{command.ListingId}/", StringComparison.Ordinal))
            .WithMessage("BlobName must be scoped under the given ListingId.")
            .When(c => c.ListingId != Guid.Empty && !string.IsNullOrEmpty(c.BlobName));
    }
}
