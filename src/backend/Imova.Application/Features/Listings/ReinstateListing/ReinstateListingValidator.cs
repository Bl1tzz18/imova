using FluentValidation;

namespace Imova.Application.Features.Listings.ReinstateListing;

public class ReinstateListingValidator : AbstractValidator<ReinstateListingCommand>
{
    public ReinstateListingValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
