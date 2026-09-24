using FluentValidation;

namespace Imova.Application.Features.Listings.MarkListingAsSold;

public class MarkListingAsSoldValidator : AbstractValidator<MarkListingAsSoldCommand>
{
    public MarkListingAsSoldValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
