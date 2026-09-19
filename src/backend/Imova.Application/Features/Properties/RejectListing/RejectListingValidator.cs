using FluentValidation;

namespace Imova.Application.Features.Properties.RejectListing;

public class RejectListingValidator : AbstractValidator<RejectListingCommand>
{
    public RejectListingValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(1000);
    }
}
