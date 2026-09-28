using FluentValidation;

namespace Imova.Application.Features.Listings.RenewListing;

public class RenewListingValidator : AbstractValidator<RenewListingCommand>
{
    public RenewListingValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
