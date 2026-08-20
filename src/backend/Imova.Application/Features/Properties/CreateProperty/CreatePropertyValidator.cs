using FluentValidation;

namespace Imova.Application.Features.Properties.CreateProperty;

public class CreatePropertyValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyValidator()
    {
        RuleFor(c => c.OwnerId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(4000);
        RuleFor(c => c.PropertyType).IsInEnum();
        RuleFor(c => c.ListingType).IsInEnum();
        RuleFor(c => c.Price).GreaterThan(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
        RuleFor(c => c.Country).NotEmpty().MaximumLength(100);
        RuleFor(c => c.City).NotEmpty().MaximumLength(100);
        RuleFor(c => c.District).MaximumLength(100);
        RuleFor(c => c.Latitude).InclusiveBetween(-90, 90);
        RuleFor(c => c.Longitude).InclusiveBetween(-180, 180);
    }
}
