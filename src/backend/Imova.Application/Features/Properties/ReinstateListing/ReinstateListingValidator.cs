using FluentValidation;

namespace Imova.Application.Features.Properties.ReinstateListing;

public class ReinstateListingValidator : AbstractValidator<ReinstateListingCommand>
{
    public ReinstateListingValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
