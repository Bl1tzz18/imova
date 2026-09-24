using FluentValidation;

namespace Imova.Application.Features.Listings.MarkListingAsRented;

public class MarkListingAsRentedValidator : AbstractValidator<MarkListingAsRentedCommand>
{
    public MarkListingAsRentedValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
