using System.Text.RegularExpressions;
using FluentValidation;

namespace Imova.Application.Features.Media.ConfirmMediaUpload;

public class ConfirmMediaUploadValidator : AbstractValidator<ConfirmMediaUploadCommand>
{
    public ConfirmMediaUploadValidator()
    {
        RuleFor(c => c.ListingId).NotEmpty();
        RuleFor(c => c.BlobName).NotEmpty();

        // A client can only confirm a blob of exactly the shape RequestUploadUrl hands out —
        // "{listingId}/{guid}{.ext}" under the listing it claims — before storage is even asked.
        // Exact, not just a prefix: a name like "{listingId}/../{other}/x.jpg" must never reach
        // storage (confirm may delete a refused upload).
        RuleFor(c => c.BlobName)
            .Must((command, blobName) => IsBlobNameFor(command.ListingId, blobName))
            .WithMessage("BlobName must be scoped under the given ListingId.")
            .When(c => c.ListingId != Guid.Empty && !string.IsNullOrEmpty(c.BlobName));
    }

    private static readonly Regex GeneratedName = new(
        @"^(?<listing>[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\.[a-z0-9]{2,5}$",
        RegexOptions.CultureInvariant);

    private static bool IsBlobNameFor(Guid listingId, string blobName)
    {
        var match = GeneratedName.Match(blobName);
        return match.Success && match.Groups["listing"].Value == listingId.ToString();
    }
}
