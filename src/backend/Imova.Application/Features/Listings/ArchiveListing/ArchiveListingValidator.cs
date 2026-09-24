using FluentValidation;

namespace Imova.Application.Features.Listings.ArchiveListing;

public class ArchiveListingValidator : AbstractValidator<ArchiveListingCommand>
{
    public ArchiveListingValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
