using FluentValidation;

namespace Imova.Application.Features.Listings.PublishListing;

public class PublishListingValidator : AbstractValidator<PublishListingCommand>
{
    public PublishListingValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
