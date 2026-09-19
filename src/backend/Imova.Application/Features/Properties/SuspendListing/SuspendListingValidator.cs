using FluentValidation;

namespace Imova.Application.Features.Properties.SuspendListing;

public class SuspendListingValidator : AbstractValidator<SuspendListingCommand>
{
    public SuspendListingValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(1000);
    }
}
